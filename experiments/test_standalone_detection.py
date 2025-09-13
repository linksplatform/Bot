#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Standalone test script for off-topic detection logic."""
import re
from typing import List, Optional, Tuple
from urllib.parse import urlparse, quote_plus
import requests
from time import sleep

# Mock config for testing
class MockConfig:
    PROGRAMMING_WEBSITES_WHITELIST = [
        'stackoverflow.com',
        'github.com',
        'developer.mozilla.org',
        'docs.python.org',
        'docs.oracle.com',
        'cppreference.com',
        'rust-lang.org',
        'golang.org',
        'w3schools.com',
        'geeksforgeeks.org',
        'medium.com',
        'dev.to',
        'reddit.com/r/programming',
        'reddit.com/r/python'
    ]
    OFF_TOPIC_DETECTION_ENABLED = True
    OFF_TOPIC_MIN_WORDS = 3

config = MockConfig()

class StandaloneOffTopicDetector:
    """Standalone version for testing."""
    
    def __init__(self):
        self.session = requests.Session()
        self.session.headers.update({
            'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36'
        })
    
    def is_message_long_enough(self, message: str) -> bool:
        words = message.strip().split()
        return len(words) >= config.OFF_TOPIC_MIN_WORDS
    
    def google_search(self, query: str, max_results: int = 10) -> List[str]:
        """Mock search implementation for testing."""
        programming_keywords = [
            'python', 'javascript', 'java', 'c++', 'c#', 'php', 'ruby', 'go', 'rust',
            'programming', 'code', 'coding', 'development', 'software', 'algorithm',
            'function', 'variable', 'class', 'method', 'api', 'framework', 'library',
            'debug', 'error', 'exception', 'syntax', 'compile', 'database', 'sql',
            'html', 'css', 'react', 'angular', 'vue', 'django', 'flask', 'spring',
            'git', 'github', 'repository', 'commit', 'merge', 'branch', 'version',
            'test', 'testing', 'unit test', 'integration', 'deployment', 'server',
            'binary search', 'tree', 'hooks', 'comprehension'
        ]
        
        query_lower = query.lower()
        urls = []
        
        print(f"  Mock searching for: '{query}'")
        
        # Check if query contains programming-related terms
        has_programming_terms = any(keyword in query_lower for keyword in programming_keywords)
        print(f"  Has programming terms: {has_programming_terms}")
        
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
        
        print(f"  Simulated {len(urls)} URLs")
        return urls[:max_results]
    
    def extract_domain(self, url: str) -> Optional[str]:
        try:
            parsed = urlparse(url if url.startswith('http') else f'http://{url}')
            domain = parsed.netloc.lower()
            if domain.startswith('www.'):
                domain = domain[4:]
            return domain
        except Exception:
            return None
    
    def check_programming_websites(self, urls: List[str]) -> Tuple[bool, List[str]]:
        matching_domains = []
        
        print(f"  Checking {len(urls)} URLs against whitelist...")
        for url in urls:
            domain = self.extract_domain(url)
            if domain:
                print(f"    - {domain}")
                for whitelist_domain in config.PROGRAMMING_WEBSITES_WHITELIST:
                    if domain == whitelist_domain or domain.endswith('.' + whitelist_domain):
                        matching_domains.append(domain)
                        print(f"      ✅ MATCH: {whitelist_domain}")
                        break
        
        return len(matching_domains) > 0, matching_domains
    
    def is_off_topic(self, message: str) -> Tuple[bool, Optional[str]]:
        if not config.OFF_TOPIC_DETECTION_ENABLED:
            return False, "Detection disabled"
            
        if not self.is_message_long_enough(message):
            return False, f"Too short ({len(message.split())} words < {config.OFF_TOPIC_MIN_WORDS})"
            
        clean_message = re.sub(r'[^\w\s]', ' ', message).strip()
        if not clean_message:
            return False, "No searchable content"
            
        try:
            search_urls = self.google_search(clean_message)
            
            if not search_urls:
                return True, "No search results found"
            
            is_programming, matching_domains = self.check_programming_websites(search_urls)
            
            if is_programming:
                return False, f"Programming-related (found: {', '.join(matching_domains[:3])})"
            else:
                return True, "No programming websites found in search results"
                
        except Exception as e:
            return False, f"Detection error: {str(e)}"

def main():
    print("Standalone Off-topic Detection Test")
    print("=" * 50)
    
    detector = StandaloneOffTopicDetector()
    
    test_cases = [
        "How do I implement binary search in Python",
        "What is React hooks",
        "What's the weather like today",
        "hi there",
        "Python list comprehension examples"
    ]
    
    for message in test_cases:
        print(f"\nTesting: '{message}'")
        print("-" * 30)
        
        is_off_topic, reason = detector.is_off_topic(message)
        
        print(f"Result: {'OFF-TOPIC' if is_off_topic else 'ON-TOPIC'}")
        print(f"Reason: {reason}")

if __name__ == "__main__":
    main()