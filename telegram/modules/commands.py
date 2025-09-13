# -*- coding: utf-8 -*-
"""Command handlers for Telegram bot."""

import re
from datetime import datetime, timedelta
from typing import List, Optional, Dict, Any
import wikipedia
from aiogram import types

from .storage import storage, User, KarmaVote
import config


class Commands:
    """Command handlers for the Telegram bot."""
    
    def __init__(self, bot):
        self.bot = bot
        wikipedia.set_lang('en')
    
    async def help_command(self, message: types.Message):
        """Show help message with available commands."""
        help_text = """
🤖 **LinksBot Commands**

**General Commands:**
• `/help` - Show this help message
• `/info` - Show your profile information
• `/update` - Update your profile information

**Programming Languages:**
• `/add_lang <language>` - Add programming language to your profile
• `/remove_lang <language>` - Remove programming language from your profile

**GitHub Profile:**
• `/add_github <username>` - Add GitHub profile to your account
• `/remove_github` - Remove GitHub profile from your account

**Karma System:**
• `/karma` - Show your karma (reply to message to see someone's karma)
• `/top [number]` - Show top users by karma (default: 10)
• `/bottom [number]` - Show bottom users by karma (default: 10)
• `+` (reply to message) - Vote to increase karma
• `-` (reply to message) - Vote to decrease karma

**Information:**
• `/people` - Show all chat members
• `/what_is <query>` - Search Wikipedia

**Available Programming Languages:**
Python, JavaScript, TypeScript, C++, C#, Java, Go, Rust, PHP, Ruby, Swift, Kotlin, and many more...

Use `/add_lang <language>` to add any supported language to your profile.
        """
        
        await message.reply(help_text, parse_mode='Markdown')
    
    async def info_command(self, message: types.Message):
        """Show user profile information."""
        target_user_id = message.from_user.id
        target_username = message.from_user.username or ""
        target_first_name = message.from_user.first_name or ""
        
        # If replying to a message, show info for that user
        if message.reply_to_message:
            target_user_id = message.reply_to_message.from_user.id
            target_username = message.reply_to_message.from_user.username or ""
            target_first_name = message.reply_to_message.from_user.first_name or ""
        
        user = storage.get_user(target_user_id, target_username, target_first_name)
        
        info_text = f"👤 **Profile Information**\n\n"
        info_text += f"**Name:** {user.first_name}\n"
        if user.username:
            info_text += f"**Username:** @{user.username}\n"
        info_text += f"**Karma:** {user.karma}\n"
        
        if user.programming_languages:
            langs = ", ".join(user.programming_languages)
            info_text += f"**Languages:** {langs}\n"
        else:
            info_text += "**Languages:** None added\n"
        
        if user.github_profile:
            info_text += f"**GitHub:** https://github.com/{user.github_profile}\n"
        else:
            info_text += "**GitHub:** Not set\n"
        
        await message.reply(info_text, parse_mode='Markdown')
    
    async def update_command(self, message: types.Message):
        """Update user profile."""
        user = storage.get_user(
            message.from_user.id, 
            message.from_user.username or "", 
            message.from_user.first_name or ""
        )
        
        # Update user info
        user.username = message.from_user.username or ""
        user.first_name = message.from_user.first_name or ""
        storage.update_user(user)
        
        await self.info_command(message)
    
    async def add_language_command(self, message: types.Message):
        """Add programming language to user profile."""
        args = message.get_args()
        if not args:
            await message.reply("Usage: `/add_lang <language>`", parse_mode='Markdown')
            return
        
        language = args.strip()
        
        # Check if language is supported
        if not self._is_valid_language(language):
            await message.reply(
                f"Language '{language}' is not supported. Use `/help` to see available languages.",
                parse_mode='Markdown'
            )
            return
        
        user = storage.get_user(
            message.from_user.id, 
            message.from_user.username or "", 
            message.from_user.first_name or ""
        )
        
        if language not in user.programming_languages:
            user.programming_languages.append(language)
            storage.update_user(user)
            await message.reply(f"✅ Added {language} to your profile!", parse_mode='Markdown')
        else:
            await message.reply(f"You already have {language} in your profile.", parse_mode='Markdown')
    
    async def remove_language_command(self, message: types.Message):
        """Remove programming language from user profile."""
        args = message.get_args()
        if not args:
            await message.reply("Usage: `/remove_lang <language>`", parse_mode='Markdown')
            return
        
        language = args.strip()
        user = storage.get_user(
            message.from_user.id, 
            message.from_user.username or "", 
            message.from_user.first_name or ""
        )
        
        if language in user.programming_languages:
            user.programming_languages.remove(language)
            storage.update_user(user)
            await message.reply(f"❌ Removed {language} from your profile!", parse_mode='Markdown')
        else:
            await message.reply(f"You don't have {language} in your profile.", parse_mode='Markdown')
    
    async def add_github_command(self, message: types.Message):
        """Add GitHub profile to user account."""
        args = message.get_args()
        if not args:
            await message.reply("Usage: `/add_github <username>`", parse_mode='Markdown')
            return
        
        github_username = args.strip()
        
        # Basic validation
        if not re.match(r'^[a-zA-Z0-9]([a-zA-Z0-9]|-(?!-))*[a-zA-Z0-9]$|^[a-zA-Z0-9]$', github_username):
            await message.reply("Invalid GitHub username format.", parse_mode='Markdown')
            return
        
        user = storage.get_user(
            message.from_user.id, 
            message.from_user.username or "", 
            message.from_user.first_name or ""
        )
        
        user.github_profile = github_username
        storage.update_user(user)
        
        await message.reply(
            f"✅ GitHub profile set to: https://github.com/{github_username}",
            parse_mode='Markdown'
        )
    
    async def remove_github_command(self, message: types.Message):
        """Remove GitHub profile from user account."""
        user = storage.get_user(
            message.from_user.id, 
            message.from_user.username or "", 
            message.from_user.first_name or ""
        )
        
        user.github_profile = ""
        storage.update_user(user)
        
        await message.reply("❌ GitHub profile removed from your account.", parse_mode='Markdown')
    
    async def karma_command(self, message: types.Message):
        """Show user karma."""
        target_user_id = message.from_user.id
        target_username = message.from_user.username or ""
        target_first_name = message.from_user.first_name or ""
        
        # If replying to a message, show karma for that user
        if message.reply_to_message:
            target_user_id = message.reply_to_message.from_user.id
            target_username = message.reply_to_message.from_user.username or ""
            target_first_name = message.reply_to_message.from_user.first_name or ""
        
        user = storage.get_user(target_user_id, target_username, target_first_name)
        
        await message.reply(f"⭐ **{user.first_name}'s karma:** {user.karma}", parse_mode='Markdown')
    
    async def top_command(self, message: types.Message):
        """Show top users by karma."""
        args = message.get_args()
        limit = 10
        
        if args:
            try:
                limit = int(args.strip())
                limit = min(max(limit, 1), 50)  # Limit between 1 and 50
            except ValueError:
                pass
        
        # Get chat members (in groups/channels)
        if message.chat.type in ['group', 'supergroup']:
            try:
                # In a real implementation, you'd get actual chat members
                # For now, we'll just show all users we know about
                chat_members = list(storage.users.values())
            except:
                chat_members = list(storage.users.values())
        else:
            chat_members = list(storage.users.values())
        
        # Sort by karma (descending)
        sorted_users = sorted(chat_members, key=lambda u: u.karma, reverse=True)[:limit]
        
        if not sorted_users:
            await message.reply("No users found.")
            return
        
        response = "🏆 **Top Users by Karma:**\n\n"
        for i, user in enumerate(sorted_users, 1):
            langs = ", ".join(user.programming_languages[:3]) if user.programming_languages else "None"
            response += f"{i}. {user.first_name} - **{user.karma}** karma\n"
            response += f"   Languages: {langs}\n"
            if user.github_profile:
                response += f"   GitHub: @{user.github_profile}\n"
            response += "\n"
        
        await message.reply(response, parse_mode='Markdown')
    
    async def bottom_command(self, message: types.Message):
        """Show bottom users by karma."""
        args = message.get_args()
        limit = 10
        
        if args:
            try:
                limit = int(args.strip())
                limit = min(max(limit, 1), 50)  # Limit between 1 and 50
            except ValueError:
                pass
        
        # Get chat members (in groups/channels)
        if message.chat.type in ['group', 'supergroup']:
            try:
                # In a real implementation, you'd get actual chat members
                # For now, we'll just show all users we know about
                chat_members = list(storage.users.values())
            except:
                chat_members = list(storage.users.values())
        else:
            chat_members = list(storage.users.values())
        
        # Sort by karma (ascending)
        sorted_users = sorted(chat_members, key=lambda u: u.karma)[:limit]
        
        if not sorted_users:
            await message.reply("No users found.")
            return
        
        response = "📉 **Bottom Users by Karma:**\n\n"
        for i, user in enumerate(sorted_users, 1):
            langs = ", ".join(user.programming_languages[:3]) if user.programming_languages else "None"
            response += f"{i}. {user.first_name} - **{user.karma}** karma\n"
            response += f"   Languages: {langs}\n"
            if user.github_profile:
                response += f"   GitHub: @{user.github_profile}\n"
            response += "\n"
        
        await message.reply(response, parse_mode='Markdown')
    
    async def people_command(self, message: types.Message):
        """Show all people in the chat."""
        await self.top_command(message)
    
    async def what_is_command(self, message: types.Message):
        """Search Wikipedia for a query."""
        args = message.get_args()
        if not args:
            await message.reply("Usage: `/what_is <query>`", parse_mode='Markdown')
            return
        
        query = args.strip()
        
        try:
            wikipedia.set_lang('en')
            summary = wikipedia.summary(query, sentences=3)
            page_url = wikipedia.page(query).url
            
            response = f"**{query}**\n\n{summary}\n\n[Read more on Wikipedia]({page_url})"
            await message.reply(response, parse_mode='Markdown', disable_web_page_preview=True)
        
        except wikipedia.exceptions.DisambiguationError as e:
            suggestions = ", ".join(e.options[:5])
            await message.reply(
                f"Multiple pages found for '{query}'. Did you mean: {suggestions}",
                parse_mode='Markdown'
            )
        except wikipedia.exceptions.PageError:
            await message.reply(f"No Wikipedia page found for '{query}'.", parse_mode='Markdown')
        except Exception as e:
            await message.reply("Sorry, I couldn't fetch information from Wikipedia.", parse_mode='Markdown')
    
    async def vote_karma(self, message: types.Message, positive: bool = True):
        """Handle karma voting."""
        if not message.reply_to_message:
            await message.reply("Reply to a message to vote on someone's karma!")
            return
        
        voter_id = message.from_user.id
        target_id = message.reply_to_message.from_user.id
        chat_id = message.chat.id
        
        # Can't vote for yourself
        if voter_id == target_id:
            await message.reply("You can't vote for your own karma!")
            return
        
        # Get users
        voter = storage.get_user(
            voter_id, 
            message.from_user.username or "", 
            message.from_user.first_name or ""
        )
        target = storage.get_user(
            target_id, 
            message.reply_to_message.from_user.username or "", 
            message.reply_to_message.from_user.first_name or ""
        )
        
        # Check cooldown
        if not self._can_vote(voter):
            cooldown_hours = self._get_karma_cooldown(voter.karma)
            await message.reply(
                f"You need to wait {cooldown_hours} hours between karma votes."
            )
            return
        
        # Check if user can vote negatively (only if target has non-negative karma)
        if not positive and target.karma < 0:
            await message.reply("Cannot vote negatively on users with negative karma.")
            return
        
        # Get recent votes
        recent_votes = storage.get_recent_karma_votes(target_id, chat_id, 24)
        
        # Count votes by type
        positive_votes = len([v for v in recent_votes if v.vote_type == 'positive'])
        negative_votes = len([v for v in recent_votes if v.vote_type == 'negative'])
        
        # Check if user already voted
        voter_votes = [v for v in recent_votes if v.voter_id == voter_id]
        if voter_votes:
            await message.reply("You have already voted for this user today!")
            return
        
        # Add vote
        vote_type = 'positive' if positive else 'negative'
        vote = KarmaVote(
            voter_id=voter_id,
            target_id=target_id,
            vote_type=vote_type,
            timestamp=datetime.now().isoformat(),
            chat_id=chat_id
        )
        storage.add_karma_vote(vote)
        
        # Update vote counts
        if positive:
            positive_votes += 1
        else:
            negative_votes += 1
        
        # Check if karma should be applied
        required_positive = config.POSITIVE_VOTES_PER_KARMA
        required_negative = config.NEGATIVE_VOTES_PER_KARMA
        
        karma_changed = False
        
        if positive and positive_votes >= required_positive:
            target.karma += 1
            karma_changed = True
            response = f"✅ {target.first_name} gained karma! New karma: {target.karma}"
        elif not positive and negative_votes >= required_negative:
            target.karma -= 1
            karma_changed = True
            response = f"❌ {target.first_name} lost karma! New karma: {target.karma}"
        else:
            if positive:
                needed = required_positive - positive_votes
                response = f"👍 Vote counted! {needed} more positive votes needed."
            else:
                needed = required_negative - negative_votes
                response = f"👎 Vote counted! {needed} more negative votes needed."
        
        if karma_changed:
            storage.update_user(target)
            # Update voter's last karma vote time
            voter.last_karma_vote = datetime.now().isoformat()
            storage.update_user(voter)
        
        await message.reply(response)
    
    def _is_valid_language(self, language: str) -> bool:
        """Check if programming language is supported."""
        return language in config.DEFAULT_PROGRAMMING_LANGUAGES
    
    def _can_vote(self, user: User) -> bool:
        """Check if user can vote (cooldown check)."""
        if not user.last_karma_vote:
            return True
        
        last_vote = datetime.fromisoformat(user.last_karma_vote)
        cooldown_hours = self._get_karma_cooldown(user.karma)
        cooldown_time = timedelta(hours=cooldown_hours)
        
        return datetime.now() - last_vote >= cooldown_time
    
    def _get_karma_cooldown(self, karma: int) -> float:
        """Get karma cooldown hours based on user karma."""
        for rule in config.KARMA_LIMIT_HOURS:
            min_karma = rule["min_karma"]
            max_karma = rule["max_karma"]
            
            if min_karma is None and karma <= max_karma:
                return rule["limit"]
            elif max_karma is None and karma >= min_karma:
                return rule["limit"]
            elif min_karma is not None and max_karma is not None:
                if min_karma <= karma <= max_karma:
                    return rule["limit"]
        
        return 2  # Default cooldown