# -*- coding: utf-8 -*-
"""Off-topic detection module using Google search."""
import re
from typing import List, Optional, Tuple
from urllib.parse import urlparse, quote_plus
import requests
from time import sleep

import config


class OffTopicDetector:
    """Detects off-topic messages using Google search and whitelisted websites."""
    
    def __init__(self):
        self.session = requests.Session()
        self.session.headers.update({
            'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36'
        })
    
    def is_message_long_enough(self, message: str) -> bool:
        """Check if message has enough words to warrant off-topic detection."""
        words = message.strip().split()
        return len(words) >= config.OFF_TOPIC_MIN_WORDS
    
    def google_search(self, query: str, max_results: int = 10) -> List[str]:
        """
        Perform Google search and extract URLs from results.
        Since Google blocks automated searches, this is a simplified mock implementation.
        In production, you would use Google Search API or other search services.
        
        Args:
            query: Search query
            max_results: Maximum number of URLs to extract
            
        Returns:
            List of simulated URLs based on query content
        """
        # Mock search results based on programming keywords
        programming_keywords = [
            'python', 'javascript', 'java', 'c++', 'c#', 'php', 'ruby', 'go', 'rust',
            'programming', 'code', 'coding', 'development', 'software', 'algorithm',
            'function', 'variable', 'class', 'method', 'api', 'framework', 'library',
            'debug', 'error', 'exception', 'syntax', 'compile', 'database', 'sql',
            'html', 'css', 'react', 'angular', 'vue', 'django', 'flask', 'spring',
            'git', 'github', 'repository', 'commit', 'merge', 'branch', 'version',
            'test', 'testing', 'unit test', 'integration', 'deployment', 'server'
        ]
        
        query_lower = query.lower()
        urls = []
        
        # Check if query contains programming-related terms
        has_programming_terms = any(keyword in query_lower for keyword in programming_keywords)
        
        if has_programming_terms:
            # Simulate programming-related search results
            urls.extend([
                'https://stackoverflow.com/questions/example',
                'https://github.com/user/repo',
                'https://docs.python.org/3/tutorial/',
                'https://developer.mozilla.org/en-US/docs/',
                'https://www.geeksforgeeks.org/example'
            ])
        else:
            # Simulate non-programming search results
            urls.extend([
                'https://en.wikipedia.org/wiki/Example',
                'https://www.news.com/article',
                'https://www.example.com/general-info',
                'https://www.blog.com/random-topic'
            ])
        
        return urls[:max_results]
    
    def extract_domain(self, url: str) -> Optional[str]:
        """Extract domain from URL."""
        try:
            parsed = urlparse(url if url.startswith('http') else f'http://{url}')
            domain = parsed.netloc.lower()
            # Remove www. prefix
            if domain.startswith('www.'):
                domain = domain[4:]
            return domain
        except Exception:
            return None
    
    def check_programming_websites(self, urls: List[str]) -> Tuple[bool, List[str]]:
        """
        Check if any URLs belong to whitelisted programming websites.
        
        Args:
            urls: List of URLs to check
            
        Returns:
            Tuple of (is_programming_related, matching_domains)
        """
        matching_domains = []
        
        for url in urls:
            domain = self.extract_domain(url)
            if domain:
                # Check against whitelist
                for whitelist_domain in config.PROGRAMMING_WEBSITES_WHITELIST:
                    if domain == whitelist_domain or domain.endswith('.' + whitelist_domain):
                        matching_domains.append(domain)
                        break
        
        return len(matching_domains) > 0, matching_domains
    
    def is_off_topic(self, message: str) -> Tuple[bool, Optional[str]]:
        """
        Determine if a message is off-topic by searching Google.
        
        Args:
            message: User message to check
            
        Returns:
            Tuple of (is_off_topic, reason_message)
        """
        if not config.OFF_TOPIC_DETECTION_ENABLED:
            return False, None
            
        if not self.is_message_long_enough(message):
            return False, None
            
        # Clean message for search
        clean_message = re.sub(r'[^\w\s]', ' ', message).strip()
        if not clean_message:
            return False, None
            
        try:
            # Search Google for the message
            search_urls = self.google_search(clean_message)
            
            if not search_urls:
                # If no search results, consider it potentially off-topic
                return True, "No search results found"
            
            # Check if any results are from programming websites
            is_programming, matching_domains = self.check_programming_websites(search_urls)
            
            if is_programming:
                return False, f"Programming-related (found: {', '.join(matching_domains[:3])})"
            else:
                return True, f"No programming websites found in search results"
                
        except Exception as e:
            print(f"Off-topic detection error: {e}")
            # In case of error, don't flag as off-topic
            return False, f"Detection error: {str(e)}"