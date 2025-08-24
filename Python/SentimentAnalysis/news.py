from newsapi import NewsApiClient
from textblob import TextBlob
from transformers import pipeline
import requests
import json

# Initialize API keys
NEWS_API_KEY = '89cbcb09b4fe4e57aa3c1b0be624320e'
CURRENTS_API_KEY = 'iF69Dw3hYmWnJWPbKf-37xyh8b9bt9ac0TTHw4c_whwGpnxx'
SERPAPI_API_KEY = 'b203c2837f5312c17a0bd0375f3d96b59208a2ccda1e6cd194e50b83ad7270ac'

# Initialize NewsAPI client
newsapi = NewsApiClient(api_key=NEWS_API_KEY)

# API endpoints
CURRENTS_LATEST_URL = 'https://api.currentsapi.services/v1/latest-news'
CURRENTS_SEARCH_URL = 'https://api.currentsapi.services/v1/search'
SERPAPI_NEWS_URL = 'https://serpapi.com/search'

# Set up sentiment analysis pipeline
sentiment_pipeline = pipeline('sentiment-analysis')

def fetch_serpapi_articles(query):
    params = {
        'engine': 'google_news',
        'q': query,
        'api_key': SERPAPI_API_KEY,
        'gl': 'us',
        'hl': 'en'
    }
    
    try:
        response = requests.get(SERPAPI_NEWS_URL, params=params)
        if response.status_code == 200:
            data = response.json()
            if 'news_results' in data:
                return data['news_results']
            else:
                print("No news results found in SerpApi response")
                return []
        else:
            print(f"SerpApi Error: Status code {response.status_code}")
            return []
    except Exception as e:
        print(f"Error fetching from SerpApi: {e}")
        return []

def fetch_newsapi_articles(query):
    try:
        articles = newsapi.get_everything(
            q=query,
            language='en',
            sort_by='publishedAt',
            page_size=20
        )
        if articles.get('status') == 'ok' and 'articles' in articles:
            return articles['articles']
        print("No results found in NewsAPI response")
        return []
    except Exception as e:
        print(f"Error fetching from News API: {e}")
        return []

def fetch_currents_articles(query=None):
    headers = {'Authorization': CURRENTS_API_KEY}
    
    try:
        if query:
            params = {'keywords': query, 'language': 'en'}
            response = requests.get(CURRENTS_SEARCH_URL, headers=headers, params=params)
        else:
            params = {'language': 'en'}
            response = requests.get(CURRENTS_LATEST_URL, headers=headers, params=params)
        
        if response.status_code == 200:
            data = response.json()
            if data.get('status') == 'ok' and 'news' in data:
                return data['news']
            
        print(f"Currents API Error: Status code {response.status_code}")
        return []
    except Exception as e:
        print(f"Error fetching from Currents API: {e}")
        return []

def normalize_article_format(article, source):
    if source == 'serpapi':
        return {
            'title': article.get('title', 'No title available'),
            'description': article.get('snippet', 'No description available'),
            'url': article.get('link', ''),
            'source': 'SerpApi - ' + article.get('source', {}).get('name', 'Unknown'),
            'date': article.get('date', 'No date available')
        }
    elif source == 'newsapi':
        return {
            'title': article.get('title', 'No title available'),
            'description': article.get('description', 'No description available'),
            'url': article.get('url', ''),
            'source': 'NewsAPI - ' + article.get('source', {}).get('name', 'Unknown'),
            'date': article.get('publishedAt', 'No date available')
        }
    elif source == 'currents':
        return {
            'title': article.get('title', 'No title available'),
            'description': article.get('description', 'No description available'),
            'url': article.get('url', ''),
            'source': 'Currents API - ' + article.get('source', 'Unknown'),
            'date': article.get('published', 'No date available')
        }

def get_sentiment_textblob(text):
    blob = TextBlob(text)
    sentiment = blob.sentiment.polarity
    if sentiment > 0:
        return "Positive", sentiment
    elif sentiment < 0:
        return "Negative", sentiment
    return "Neutral", sentiment

def get_sentiment_huggingface(text):
    result = sentiment_pipeline(text)
    return result[0]['label'], result[0]['score']

def analyze_articles(query):
    # Fetch articles from all three APIs
    serpapi_articles = fetch_serpapi_articles(query)
    newsapi_articles = fetch_newsapi_articles(query)
    currents_articles = fetch_currents_articles(query)
    
    # Normalize and combine articles
    all_articles = []
    all_articles.extend([normalize_article_format(a, 'serpapi') for a in serpapi_articles])
    all_articles.extend([normalize_article_format(a, 'newsapi') for a in newsapi_articles])
    all_articles.extend([normalize_article_format(a, 'currents') for a in currents_articles])
    
    # Initialize sentiment tracking
    total_textblob_score = 0
    total_huggingface_score = 0
    num_articles = len(all_articles)
    
    # Analysis results storage
    detailed_results = []
    
    # Source statistics
    source_stats = {
        'serpapi': len(serpapi_articles),
        'newsapi': len(newsapi_articles),
        'currents': len(currents_articles)
    }
    
    # Analyze each article
    for article in all_articles:
        text = f"{article['title']} {article['description']}"
        
        # Get sentiments
        sentiment_textblob, score_textblob = get_sentiment_textblob(text)
        sentiment_huggingface, score_huggingface = get_sentiment_huggingface(text)
        
        # Update totals
        total_textblob_score += score_textblob
        total_huggingface_score += score_huggingface
        
        # Store detailed analysis
        result = {
            'article': article,
            'sentiment_analysis': {
                'textblob': {'sentiment': sentiment_textblob, 'score': score_textblob},
                'huggingface': {'sentiment': sentiment_huggingface, 'score': score_huggingface}
            }
        }
        detailed_results.append(result)
        
        # Print individual article analysis
        print(f"\nSource: {article['source']}")
        print(f"Date: {article['date']}")
        print(f"Article Title: {article['title']}")
        print(f"Description: {article['description']}")
        print(f"URL: {article['url']}")
        print(f"Sentiment (TextBlob): {sentiment_textblob} (Score: {score_textblob:.2f})")
        print(f"Sentiment (Hugging Face): {sentiment_huggingface} (Score: {score_huggingface:.2f})")
        print("-" * 80)
    
    # Calculate and print overall results
    if num_articles > 0:
        avg_textblob_score = total_textblob_score / num_articles
        avg_huggingface_score = total_huggingface_score / num_articles
        
        overall_sentiment_textblob = "Positive" if avg_textblob_score > 0 else "Negative" if avg_textblob_score < 0 else "Neutral"
        overall_sentiment_huggingface = "POSITIVE" if avg_huggingface_score > 0.5 else "NEGATIVE"
        
        print("\n=== Overall Sentiment Analysis ===")
        print(f"Total Articles Analyzed: {num_articles}")
        print("\nArticles by Source:")
        for source, count in source_stats.items():
            print(f"{source}: {count} articles")
        print(f"\nTextBlob Sentiment: {overall_sentiment_textblob} (Average: {avg_textblob_score:.2f})")
        print(f"Hugging Face Sentiment: {overall_sentiment_huggingface} (Average: {avg_huggingface_score:.2f})")
        
        # Save results to JSON file
        results = {
            'query': query,
            'total_articles': num_articles,
            'source_statistics': source_stats,
            'overall_sentiment': {
                'textblob': {'sentiment': overall_sentiment_textblob, 'score': avg_textblob_score},
                'huggingface': {'sentiment': overall_sentiment_huggingface, 'score': avg_huggingface_score}
            },
            'detailed_results': detailed_results
        }
        
        with open('sentiment_analysis_results.json', 'w', encoding='utf-8') as f:
            json.dump(results, f, ensure_ascii=False, indent=2)
            
        print("\nResults have been saved to 'sentiment_analysis_results.json'")
    else:
        print("\nNo articles found to analyze.")

if __name__ == "__main__":
    search_query = input("Enter your search query: ")
    analyze_articles(search_query)