from newsapi import NewsApiClient
from textblob import TextBlob
from transformers import pipeline, AutoModelForSequenceClassification, AutoTokenizer
from spacy import load
import requests
import json
import pyodbc
import datetime
import pytz
import time
import os
import numpy as np
from typing import Dict, List, Any, Union, Optional, Tuple
from dataclasses import dataclass, asdict
from dateutil import parser
import logging
from requests.adapters import HTTPAdapter
from requests.packages.urllib3.util.retry import Retry

# Environment variables and logging setup
os.environ["HF_HUB_DISABLE_SYMLINKS_WARNING"] = "1"
os.environ["TOKENIZERS_PARALLELISM"] = "false"

logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

# API Keys and Constants
NEWSAPI_KEY = '89cbcb09b4fe4e57aa3c1b0be624320e'
SERPAPI_API_KEY = 'b203c2837f5312c17a0bd0375f3d96b59208a2ccda1e6cd194e50b83ad7270ac'

# Database configuration
DB_CONFIG = {
    'Driver': '{SQL Server}',
    'Server': '192.168.1.210',
    'Database': 'KalshiBot-Dev',
    'UID': 'KalshiBotUpdater',
    'PWD': 'YetiTheCat123'
}

# Configure requests session
session = requests.Session()
retries = Retry(total=3, backoff_factor=0.5, status_forcelist=[500, 502, 503, 504])
session.mount('https://', HTTPAdapter(max_retries=retries))

@dataclass
class Article:
    title: str
    description: str
    url: str
    source: str
    date: str

@dataclass
class MarketInfo:
    market_id: str
    market_title: str
    market_subtitle: str
    event_title: str
    series_title: str
    open_time: Optional[datetime.datetime] = None
    close_time: Optional[datetime.datetime] = None
    volume: Optional[float] = None
    liquidity: Optional[float] = None
    probability_yes: Optional[float] = None
    yes_bid: Optional[float] = None
    yes_ask: Optional[float] = None
    no_bid: Optional[float] = None
    no_ask: Optional[float] = None
    volume_24h: Optional[float] = None
    open_interest: Optional[float] = None

def get_db_connection():
    """Create database connection with error handling"""
    try:
        conn_str = ';'.join([f'{k}={v}' for k, v in DB_CONFIG.items()])
        return pyodbc.connect(conn_str)
    except Exception as e:
        logger.error(f"Failed to connect to database: {e}")
        raise
def get_market_info_from_db(market_ticker: str) -> Tuple[MarketInfo, List[str], List[Tuple[datetime.datetime, float]]]:
    """Get comprehensive market information from SQL database"""
    try:
        conn = get_db_connection()
        cursor = conn.cursor()
        
        # Get market details with associated event and series info
        cursor.execute("""
            SELECT 
                m.market_ticker,
                m.title as market_title,
                m.subtitle as market_subtitle,
                e.title as event_title,
                s.title as series_title,
                m.open_time,
                m.close_time,
                m.volume,
                m.liquidity,
                m.last_price as probability_yes,
                m.yes_bid,
                m.yes_ask,
                m.no_bid,
                m.no_ask,
                m.volume_24h,
                m.open_interest,
                e.category as event_category,
                s.category as series_category
            FROM t_Markets m
            JOIN t_Events e ON m.event_ticker = e.event_ticker
            JOIN t_Series s ON e.series_ticker = s.series_ticker
            WHERE m.market_ticker = ?
        """, market_ticker)
        
        columns = [column[0] for column in cursor.description]
        row = cursor.fetchone()
        
        if not row:
            raise ValueError(f"Market {market_ticker} not found in database")
        
        # Create a dictionary from row data
        row_dict = dict(zip(columns, row))
            
        market_info = MarketInfo(
            market_id=row_dict['market_ticker'],
            market_title=row_dict['market_title'],
            market_subtitle=row_dict['market_subtitle'],
            event_title=row_dict['event_title'],
            series_title=row_dict['series_title'],
            open_time=row_dict['open_time'],
            close_time=row_dict['close_time'],
            volume=row_dict['volume'],
            liquidity=row_dict['liquidity'],
            probability_yes=row_dict['probability_yes'],
            yes_bid=row_dict['yes_bid'],
            yes_ask=row_dict['yes_ask'],
            no_bid=row_dict['no_bid'],
            no_ask=row_dict['no_ask'],
            volume_24h=row_dict['volume_24h'],
            open_interest=row_dict['open_interest']
        )
        
        # Get candlestick data for price history using TOP instead of LIMIT
        cursor.execute("""
            SELECT TOP 100 end_period_ts, price_close
            FROM t_Candlesticks
            WHERE market_ticker = ?
            AND interval_type = 1  -- Use minute interval data
            ORDER BY end_period_ts DESC
        """, market_ticker)
        
        price_history = [(row[0], row[1]) for row in cursor.fetchall()]
        
        # Build search terms from all available data
        search_terms = set()
        search_terms.update([
            term.strip() for term in [
                row_dict['market_title'],
                row_dict['market_subtitle'],
                row_dict['event_title'],
                row_dict['series_title'],
                row_dict['event_category'],
                row_dict['series_category']
            ] if term and term.strip()
        ])
        
        return market_info, list(search_terms), price_history
        
    except Exception as e:
        logger.error(f"Database error: {e}")
        raise
    finally:
        if 'cursor' in locals():
            cursor.close()
        if 'conn' in locals():
            conn.close()
                     
def initialize_nlp_components():
    """Initialize NLP components"""
    try:
        nlp = load('en_core_web_lg')
        newsapi = NewsApiClient(api_key=NEWSAPI_KEY)
        
        model_name = "distilbert-base-uncased-finetuned-sst-2-english"
        model = AutoModelForSequenceClassification.from_pretrained(model_name)
        tokenizer = AutoTokenizer.from_pretrained(model_name)
        sentiment_pipeline = pipeline(
            task='sentiment-analysis',
            model=model,
            tokenizer=tokenizer,
            device=-1
        )
        
        logger.info("Successfully initialized all NLP components")
        return nlp, newsapi, sentiment_pipeline
        
    except Exception as e:
        logger.error(f"Error initializing NLP components: {e}")
        raise

def fetch_serpapi_articles(query: str) -> List[Article]:
    """Fetch articles using SerpAPI"""
    params = {
        'q': query,
        'api_key': SERPAPI_API_KEY,
        'num': 100,
        'tbm': 'nws'
    }
    
    try:
        response = session.get('https://serpapi.com/search', params=params)
        response.raise_for_status()
        data = response.json()
        
        articles = []
        for item in data.get('news_results', []):
            article = Article(
                title=item.get('title', ''),
                description=item.get('snippet', ''),
                url=item.get('link', ''),
                source=f"SerpAPI - {item.get('source', 'Unknown')}",
                date=item.get('date', '')
            )
            articles.append(article)
        
        return articles
        
    except Exception as e:
        logger.error(f"SerpAPI request error: {e}")
        return []

def fetch_newsapi_articles(query: str) -> List[Article]:
    """Fetch articles from NewsAPI"""
    try:
        response = newsapi.get_everything(
            q=query,
            language='en',
            sort_by='relevancy',
            page_size=20
        )
        
        articles = []
        for item in response.get('articles', []):
            article = Article(
                title=item.get('title', ''),
                description=item.get('description', ''),
                url=item.get('url', ''),
                source=f"NewsAPI - {item.get('source', {}).get('name', 'Unknown')}",
                date=item.get('publishedAt', '')
            )
            articles.append(article)
            
        return articles
        
    except Exception as e:
        logger.error(f"NewsAPI request error: {e}")
        return []

def fetch_all_news_sources(query: str) -> List[Article]:
    """Fetch and combine news from all sources"""
    all_articles = []
    
    news_sources = {
        'SerpAPI': fetch_serpapi_articles,
        'NewsAPI': fetch_newsapi_articles
    }
    
    for source_name, fetch_func in news_sources.items():
        try:
            logger.info(f"Fetching from {source_name}...")
            articles = fetch_func(query)
            
            if articles:
                logger.info(f"Retrieved {len(articles)} articles from {source_name}")
                all_articles.extend(articles)
            else:
                logger.warning(f"No articles retrieved from {source_name}")
                
        except Exception as e:
            logger.error(f"Error fetching from {source_name}: {e}")
            continue
    
    seen_urls = set()
    unique_articles = []
    
    for article in all_articles:
        if article.url not in seen_urls:
            seen_urls.add(article.url)
            unique_articles.append(article)
    
    logger.info(f"Total unique articles after deduplication: {len(unique_articles)}")
    return unique_articles

def analyze_sentiment(text: str) -> Dict[str, float]:
    """Analyze sentiment using multiple approaches"""
    try:
        blob = TextBlob(text)
        blob_sentiment = blob.sentiment
        
        hf_result = sentiment_pipeline(text)[0]
        
        return {
            'textblob_polarity': blob_sentiment.polarity,
            'textblob_subjectivity': blob_sentiment.subjectivity,
            'huggingface_label': hf_result['label'],
            'huggingface_score': hf_result['score']
        }
    except Exception as e:
        logger.error(f"Error in sentiment analysis: {e}")
        return {
            'textblob_polarity': 0,
            'textblob_subjectivity': 0,
            'huggingface_label': 'NEUTRAL',
            'huggingface_score': 0.5
        }

def calculate_market_metrics(price_history: List[Tuple[datetime.datetime, float]]) -> Dict[str, float]:
    """Calculate market metrics from historical price data"""
    if not price_history:
        return {}
        
    prices = [price for _, price in price_history]
    timestamps = [ts for ts, _ in price_history]
    
    metrics = {
        'price_volatility': float(np.std(prices)) if len(prices) > 1 else 0,
        'price_momentum': float(np.mean(np.diff(prices))) if len(prices) > 1 else 0,
        'trading_volume': len(price_history),
        'time_to_close': (max(timestamps) - min(timestamps)).total_seconds() / 3600 if len(timestamps) > 1 else 0
    }
    
    return {k: v for k, v in metrics.items() if not np.isnan(v)}

def analyze_market(market_ticker: str, output_file: Optional[str] = None) -> Dict[str, Any]:
    """Main analysis function for a market"""
    try:
        logger.info(f"Starting analysis for market: {market_ticker}")
        
        market_info, search_terms, price_history = get_market_info_from_db(market_ticker)
        market_metrics = calculate_market_metrics(price_history)
        
        all_articles = []
        for term in search_terms:
            articles = fetch_all_news_sources(term)
            all_articles.extend(articles)
        
        results = []
        for article in all_articles:
            sentiment = analyze_sentiment(f"{article.title} {article.description}")
            result = {
                'article': asdict(article),
                'sentiment': sentiment
            }
            results.append(result)
        
        analysis_results = {
            'market_info': asdict(market_info),
            'market_metrics': market_metrics,
            'search_terms': search_terms,
            'results': results
        }
        
        if output_file:
            with open(output_file, 'w') as f:
                json.dump(analysis_results, f, indent=2, default=str)
        
        print_analysis_summary(analysis_results)
        return analysis_results
        
    except Exception as e:
        logger.error(f"Error in market analysis: {e}")
        return {'error': str(e)}

def print_analysis_summary(results: Dict[str, Any]):
    """Print formatted analysis summary"""
    print("\n=== Analysis Results ===")
    
    # Market Info
    print("\nMarket Info:")
    market_info = results['market_info']
    for key, value in market_info.items():
        if value is not None:
            print(f"{key}: {value}")
    
    # Market Metrics
    if results['market_metrics']:
        print("\nMarket Metrics:")
        for key, value in results['market_metrics'].items():
            print(f"{key}: {value:.3f}")
    
    # Search Terms
    print("\nKey Search Terms Used:")
    for term in results['search_terms']:
        print(f"- {term}")
    
    # Article Analysis
    articles = results['results']
    total_articles = len(articles)
    
    if total_articles > 0:
        avg_polarity = sum(r['sentiment']['textblob_polarity'] for r in articles) / total_articles
        avg_subjectivity = sum(r['sentiment']['textblob_subjectivity'] for r in articles) / total_articles
        
        print(f"\nArticle Analysis Summary:")
        print(f"Total articles analyzed: {total_articles}")
        print(f"\nOverall Sentiment Analysis:")
        print(f"Average Polarity: {avg_polarity:.3f} (-1 negative to 1 positive)")
        print(f"Average Subjectivity: {avg_subjectivity:.3f} (0 objective to 1 subjective)")
        
        # Display top articles
        sorted_articles = sorted(articles, key=lambda x: x['sentiment']['textblob_polarity'], reverse=True)
        
        print("\nTop 5 Most Positive Articles:")
        for article in sorted_articles[:5]:
            print(f"\nTitle: {article['article']['title']}")
            print(f"Source: {article['article']['source']}")
            print(f"Sentiment: {article['sentiment']['textblob_polarity']:.3f}")
            print(f"URL: {article['article']['url']}")
        
        print("\nTop 5 Most Negative Articles:")
        for article in sorted_articles[-5:]:
            print(f"\nTitle: {article['article']['title']}")
            print(f"Source: {article['article']['source']}")
            print(f"Sentiment: {article['sentiment']['textblob_polarity']:.3f}")
            print(f"URL: {article['article']['url']}")

def main(market_ticker: str, output_file: Optional[str] = None) -> Dict[str, Any]:
    """Main execution function"""
    try:
        return analyze_market(market_ticker, output_file)
    except Exception as e:
        logger.error(f"Error in main execution: {e}")
        return {'error': str(e)}

if __name__ == "__main__":
    import sys
    
    try:
        # Initialize NLP components
        nlp, newsapi, sentiment_pipeline = initialize_nlp_components()
        
        if len(sys.argv) < 2:
            market_ticker = input("Please enter market ticker: ")
            output_file = input("Please enter output file name (optional, press Enter to skip): ").strip() or None
        else:
            market_ticker = sys.argv[1]
            output_file = sys.argv[2] if len(sys.argv) > 2 else None
        
        logger.info(f"Starting analysis for market ticker: {market_ticker}")
        results = main(market_ticker, output_file)
        
        if 'error' in results:
            logger.error(f"Analysis failed: {results['error']}")
            sys.exit(1)
        else:
            logger.info("Analysis completed successfully")
            
    except KeyboardInterrupt:
        logger.info("Analysis interrupted by user")
        sys.exit(0)
    except Exception as e:
        logger.error(f"Unexpected error: {e}")
        sys.exit(1)