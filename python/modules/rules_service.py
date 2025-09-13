# -*- coding: utf-8 -*-
import json
import os
from typing import Dict, Optional, Any, NoReturn
from datetime import datetime
import requests


class RulesService:
    """Service for managing chat rules from GitHub Gists"""
    
    def __init__(self, config_file: str = "chat_rules.json"):
        self.config_file = config_file
        self.rules_config = self._load_config()
    
    def _load_config(self) -> Dict[str, Dict[str, Any]]:
        """Load rules configuration from file"""
        if os.path.exists(self.config_file):
            try:
                with open(self.config_file, 'r', encoding='utf-8') as f:
                    return json.load(f)
            except (json.JSONDecodeError, IOError):
                pass
        return {}
    
    def _save_config(self) -> NoReturn:
        """Save rules configuration to file"""
        try:
            with open(self.config_file, 'w', encoding='utf-8') as f:
                json.dump(self.rules_config, f, ensure_ascii=False, indent=2)
        except IOError as e:
            print(f"Error saving rules config: {e}")
    
    def set_rules_gist(self, peer_id: int, gist_url: str, user_id: int) -> bool:
        """Set GitHub gist URL for chat rules
        
        Args:
            peer_id: Chat ID
            gist_url: GitHub gist URL
            user_id: User ID who set the rules
            
        Returns:
            True if successful, False otherwise
        """
        try:
            # Extract gist ID from URL
            if "/gist.github.com/" in gist_url:
                parts = gist_url.split("/")
                gist_id = parts[-1]
                
                # Test if gist is accessible
                if self._test_gist_access(gist_id):
                    peer_id_str = str(peer_id)
                    self.rules_config[peer_id_str] = {
                        "gist_url": gist_url,
                        "gist_id": gist_id,
                        "set_by": user_id,
                        "set_at": datetime.now().isoformat(),
                        "last_check": None,
                        "last_content_hash": None,
                        "pinned_message_id": None
                    }
                    self._save_config()
                    return True
            return False
        except Exception as e:
            print(f"Error setting rules gist: {e}")
            return False
    
    def remove_rules_gist(self, peer_id: int) -> bool:
        """Remove GitHub gist URL for chat rules"""
        try:
            peer_id_str = str(peer_id)
            if peer_id_str in self.rules_config:
                del self.rules_config[peer_id_str]
                self._save_config()
                return True
            return False
        except Exception as e:
            print(f"Error removing rules gist: {e}")
            return False
    
    def get_rules_config(self, peer_id: int) -> Optional[Dict[str, Any]]:
        """Get rules configuration for a chat"""
        return self.rules_config.get(str(peer_id))
    
    def _test_gist_access(self, gist_id: str) -> bool:
        """Test if gist is accessible"""
        try:
            url = f"https://api.github.com/gists/{gist_id}"
            response = requests.get(url, timeout=10)
            return response.status_code == 200
        except Exception:
            return False
    
    def fetch_gist_content(self, gist_id: str) -> Optional[str]:
        """Fetch content from GitHub gist"""
        try:
            url = f"https://api.github.com/gists/{gist_id}"
            response = requests.get(url, timeout=10)
            if response.status_code == 200:
                data = response.json()
                # Get the first file's content
                files = data.get('files', {})
                if files:
                    first_file = next(iter(files.values()))
                    return first_file.get('content', '')
            return None
        except Exception as e:
            print(f"Error fetching gist content: {e}")
            return None
    
    def check_gist_updates(self, peer_id: int) -> Optional[str]:
        """Check if gist content has been updated
        
        Returns:
            New content if updated, None if no update or error
        """
        config = self.get_rules_config(peer_id)
        if not config:
            return None
        
        try:
            gist_id = config["gist_id"]
            current_content = self.fetch_gist_content(gist_id)
            
            if current_content is not None:
                # Calculate simple hash of content
                content_hash = hash(current_content.strip())
                last_hash = config.get("last_content_hash")
                
                # Update last check time
                peer_id_str = str(peer_id)
                self.rules_config[peer_id_str]["last_check"] = datetime.now().isoformat()
                
                if last_hash != content_hash:
                    # Content has changed
                    self.rules_config[peer_id_str]["last_content_hash"] = content_hash
                    self._save_config()
                    return current_content
                else:
                    self._save_config()
            return None
        except Exception as e:
            print(f"Error checking gist updates: {e}")
            return None
    
    def update_pinned_message_id(self, peer_id: int, message_id: int) -> NoReturn:
        """Update the stored pinned message ID"""
        peer_id_str = str(peer_id)
        if peer_id_str in self.rules_config:
            self.rules_config[peer_id_str]["pinned_message_id"] = message_id
            self._save_config()
    
    def get_all_monitored_chats(self) -> Dict[str, Dict[str, Any]]:
        """Get all chats with rules monitoring configured"""
        return self.rules_config