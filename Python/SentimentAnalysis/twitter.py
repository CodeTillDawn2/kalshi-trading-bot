import requests
import pandas as pd
import os
import time
import random
from datetime import datetime
from bs4 import BeautifulSoup
from urllib.parse import quote
from textblob import TextBlob
from vaderSentiment.vaderSentiment import SentimentIntensityAnalyzer
from transformers import pipeline, AutoTokenizer
import warnings
warnings.filterwarnings('ignore')

class TweetCollector:
    def __init__(self):
        self.session = requests.Session()
        
        # List of common User-Agents to rotate
        self.user_agents = [
            'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36',
            'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.114 Safari/537.36',
            'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Firefox/89.0 Safari/537.36',
            'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Edge/91.0.864.59 Safari/537.36'
        ]
        
        # Set initial headers with a random User-Agent
        self.headers = {
            'User-Agent': random.choice(self.user_agents),
            'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8',
            'Accept-Language': 'en-US,en;q=0.5'
        }
        
        self.base_url = 'https://lightbrd.com'
        self.connection_timeout = 10
        self.read_timeout = 20
        
        # Initialize sentiment analyzers
        print("Initializing sentiment analyzers...")
        self.vader = SentimentIntensityAnalyzer()
        self.model_name = "finiteautomata/bertweet-base-sentiment-analysis"
        self.tokenizer = AutoTokenizer.from_pretrained(self.model_name)
        self.hugging = pipeline("sentiment-analysis", model=self.model_name, tokenizer=self.tokenizer)
        print("Device set to use cpu")
        
        # Add set to track unique tweets
        self.seen_tweets = set()

    def get_tweets(self, query="pastured chicken", pages=20):
        all_tweets = []
        self.seen_tweets.clear()
        last_tweet_count = 0
        no_new_tweets_count = 0
        
        for page in range(1, pages + 1):
            try:
                url = f"{self.base_url}/search?f=tweets&q={quote(query)}&p={page}"
                print(f"\nFetching page {page}")
                
                # Randomize the User-Agent on each request
                self.headers['User-Agent'] = random.choice(self.user_agents)
                
                response = self.session.get(
                    url,
                    headers=self.headers,
                    timeout=(self.connection_timeout, self.read_timeout),
                    verify=False
                )
                
                if response.status_code == 200:
                    soup = BeautifulSoup(response.text, 'html.parser')
                    tweets = soup.select('.timeline-item')
                    
                    for tweet in tweets:
                        tweet_data = self._extract_tweet(tweet)
                        if tweet_data:
                            all_tweets.append(tweet_data)
                            print(f"Found unique tweet from {tweet_data['date']}")

                    # Check if we're getting new tweets
                    if len(all_tweets) == last_tweet_count:
                        no_new_tweets_count += 1
                        if no_new_tweets_count >= 3:  # Stop if 3 consecutive pages yield no new tweets
                            print("\nNo new tweets found in last 3 pages. Stopping collection...")
                            break
                    else:
                        no_new_tweets_count = 0
                        
                    last_tweet_count = len(all_tweets)
                    
                    # Add a random delay between requests to avoid rate-limiting
                    delay = random.uniform(3, 6)  # Longer delay
                    print(f"Sleeping for {delay:.2f} seconds...")
                    time.sleep(delay)
                    
                else:
                    print(f"Got status code {response.status_code} for page {page}")
                    time.sleep(5)  # Longer delay on error
                    
            except Exception as e:
                print(f"Error on page {page}: {str(e)}")
                time.sleep(5)  # Longer delay on error
                
            # Progress update
            print(f"Collected {len(all_tweets)} unique tweets so far...")
                
        print(f"\nFinished collecting {len(all_tweets)} unique tweets across {page} pages")
        return all_tweets

    def _extract_tweet(self, tweet):
        try:
            content = tweet.select_one('.tweet-content')
            if not content:
                return None
                
            text = content.get_text().strip()
            
            # Skip if we've seen this tweet before
            if text in self.seen_tweets:
                return None
            self.seen_tweets.add(text)
            
            # Extract date and URL
            date_elem = tweet.select_one('.tweet-date a')
            date = date_elem.get('title') if date_elem else ''
            url = self.base_url + date_elem.get('href') if date_elem else ''
            
            # Process sentiment
            vader_scores = self.vader.polarity_scores(text)
            blob = TextBlob(text)
            
            # Handle long tweets for HuggingFace
            try:
                inputs = self.tokenizer(text, truncation=True, max_length=128, return_tensors="pt")
                truncated_text = self.tokenizer.decode(inputs['input_ids'][0], skip_special_tokens=True)
                hugging_result = self.hugging(truncated_text)[0]
            except Exception as e:
                print(f"HuggingFace error for tweet: {str(e)}")
                hugging_result = {'label': 'NEU', 'score': 0.5}
            
            return {
                'text': text,
                'date': date,
                'url': url,
                'vader_compound': vader_scores['compound'],
                'vader_pos': vader_scores['pos'],
                'vader_neg': vader_scores['neg'],
                'vader_neu': vader_scores['neu'],
                'textblob_polarity': blob.sentiment.polarity,
                'textblob_subjectivity': blob.sentiment.subjectivity,
                'huggingface_sentiment': hugging_result['label'],
                'huggingface_score': hugging_result['score'],
                'truncated': len(text.split()) > 128
            }
            
        except Exception as e:
            print(f"Error extracting tweet: {str(e)}")
            return None

def main():
    output_dir = r"C:\Users\Sheesh\AppData\Local\Programs\Python\Python312\Lib\projects\kalshi-bot\kalshi-bot\SentimentAnalysis"
    output_file = os.path.join(output_dir, f"tweet_analysis_unique_{datetime.now().strftime('%Y%m%d_%H%M%S')}.csv")
    
    collector = TweetCollector()
    print("\nCollecting unique tweets about pastured chicken...")
    tweets = collector.get_tweets()
    
    if tweets:
        df = pd.DataFrame(tweets)
        
        # Sort by date for better analysis
        df['date'] = pd.to_datetime(df['date'], format='%b %d, %Y · %I:%M %p UTC')
        df = df.sort_values('date', ascending=False)
        
        df.to_csv(output_file, index=False)
        print(f"\nSaved {len(tweets)} unique tweets to {output_file}")
        
        # Display detailed summary
        print("\nSentiment Analysis Summary:")
        print(f"Average VADER compound: {df['vader_compound'].mean():.3f}")
        print(f"Average TextBlob polarity: {df['textblob_polarity'].mean():.3f}")
        print("\nHuggingFace Sentiment Distribution:")
        print(df['huggingface_sentiment'].value_counts())
        print("\nDate Range:")
        print(f"Earliest tweet: {df['date'].min()}")
        print(f"Latest tweet: {df['date'].max()}")
        
        # Print truncation statistics
        if 'truncated' in df.columns:
            truncated_count = df['truncated'].sum()
            print(f"\nTruncated tweets: {truncated_count} of {len(df)} ({truncated_count/len(df)*100:.1f}%)")
        
    else:
        print("No tweets collected")

if __name__ == "__main__":
    main()