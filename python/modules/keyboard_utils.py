# -*- coding: utf-8 -*-
"""Utility module for creating VK keyboards and handling callback buttons."""
import json
from typing import Dict, Any, Optional, List


class VkKeyboard:
    """VK Keyboard builder for creating inline keyboards with callback buttons."""
    
    def __init__(self, inline: bool = True, one_time: bool = False):
        """Initialize keyboard builder.
        
        Args:
            inline: Whether to create inline keyboard (displayed inside message)
            one_time: Whether keyboard disappears after button press
        """
        self.inline = inline
        self.one_time = one_time
        self.buttons: List[List[Dict[str, Any]]] = []
        self.current_row: List[Dict[str, Any]] = []
    
    def add_callback_button(self, 
                          label: str, 
                          payload: Dict[str, Any], 
                          color: str = "secondary") -> "VkKeyboard":
        """Add callback button to current row.
        
        Args:
            label: Text displayed on button
            payload: Data to send with callback
            color: Button color (primary, secondary, negative, positive)
        
        Returns:
            Self for method chaining
        """
        button = {
            "action": {
                "type": "callback",
                "label": label,
                "payload": json.dumps(payload)
            },
            "color": color
        }
        self.current_row.append(button)
        return self
    
    def add_text_button(self,
                       label: str,
                       payload: Optional[Dict[str, Any]] = None,
                       color: str = "secondary") -> "VkKeyboard":
        """Add text button to current row.
        
        Args:
            label: Text displayed on button and sent as message
            payload: Optional data to include
            color: Button color
            
        Returns:
            Self for method chaining
        """
        button = {
            "action": {
                "type": "text",
                "label": label
            },
            "color": color
        }
        if payload:
            button["action"]["payload"] = json.dumps(payload)
        self.current_row.append(button)
        return self
    
    def row(self) -> "VkKeyboard":
        """Finish current row and start new one.
        
        Returns:
            Self for method chaining
        """
        if self.current_row:
            self.buttons.append(self.current_row)
            self.current_row = []
        return self
    
    def get_keyboard(self) -> str:
        """Get keyboard as JSON string for VK API.
        
        Returns:
            JSON string representation of keyboard
        """
        # Add current row if it has buttons
        if self.current_row:
            self.buttons.append(self.current_row)
            self.current_row = []
        
        keyboard = {
            "inline": self.inline,
            "one_time": self.one_time,
            "buttons": self.buttons
        }
        return json.dumps(keyboard)
    
    @staticmethod
    def get_empty_keyboard() -> str:
        """Get empty keyboard to hide existing keyboard.
        
        Returns:
            JSON string for empty keyboard
        """
        return json.dumps({"buttons": []})


class TopPaginationKeyboard:
    """Specialized keyboard for top command pagination."""
    
    USERS_PER_PAGE = 10
    
    @staticmethod
    def create_pagination_keyboard(current_page: int, 
                                 total_pages: int,
                                 command_type: str = "top",
                                 reverse: bool = False) -> str:
        """Create pagination keyboard for top command.
        
        Args:
            current_page: Current page number (0-indexed)
            total_pages: Total number of pages
            command_type: Type of command (top, bottom, people)
            reverse: Whether results are reversed
            
        Returns:
            JSON keyboard string
        """
        keyboard = VkKeyboard(inline=True)
        
        # Add navigation buttons
        if current_page > 0:
            keyboard.add_callback_button(
                "⬅️ Пред.",
                {
                    "action": "paginate",
                    "command": command_type,
                    "page": current_page - 1,
                    "reverse": reverse
                }
            )
        
        # Page indicator button (non-clickable, shows current page)
        keyboard.add_callback_button(
            f"{current_page + 1}/{total_pages}",
            {
                "action": "page_info",
                "page": current_page
            },
            color="primary"
        )
        
        if current_page < total_pages - 1:
            keyboard.add_callback_button(
                "След. ➡️",
                {
                    "action": "paginate", 
                    "command": command_type,
                    "page": current_page + 1,
                    "reverse": reverse
                }
            )
        
        return keyboard.get_keyboard()
    
    @staticmethod
    def calculate_pagination(total_users: int, users_per_page: int = None) -> Dict[str, int]:
        """Calculate pagination parameters.
        
        Args:
            total_users: Total number of users
            users_per_page: Users per page (defaults to USERS_PER_PAGE)
            
        Returns:
            Dictionary with pagination info
        """
        if users_per_page is None:
            users_per_page = TopPaginationKeyboard.USERS_PER_PAGE
            
        total_pages = max(1, (total_users + users_per_page - 1) // users_per_page)
        
        return {
            "total_users": total_users,
            "users_per_page": users_per_page,
            "total_pages": total_pages
        }
    
    @staticmethod
    def get_page_users(users: List[Any], page: int, users_per_page: int = None) -> List[Any]:
        """Get users for specific page.
        
        Args:
            users: List of all users
            page: Page number (0-indexed)
            users_per_page: Users per page
            
        Returns:
            List of users for the page
        """
        if users_per_page is None:
            users_per_page = TopPaginationKeyboard.USERS_PER_PAGE
            
        start_idx = page * users_per_page
        end_idx = start_idx + users_per_page
        return users[start_idx:end_idx]