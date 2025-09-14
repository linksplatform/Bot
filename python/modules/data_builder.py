# -*- coding: utf-8 -*-
from typing import List

from social_ethosa import BetterUser
from saya import Vk

from .data_service import BetterBotBaseDataService
import config


class DataBuilder:
    @staticmethod
    def build_programming_languages(
        user: BetterUser,
        data: BetterBotBaseDataService,
        default: str = "отсутствуют"
    ) -> str:
        """Builds the user's programming languages and returns its string representation.
        """
        languages = data.get_user_sorted_programming_languages(user)
        return ", ".join(languages) if len(languages) > 0 else default

    @staticmethod
    def build_github_profile(
        user: BetterUser,
        data: BetterBotBaseDataService,
        default: str = "",
        prefix: str = ""
    ) -> str:
        """Builds the user's github profile and returns its string representation.
        """
        profile = user["github_profile"]
        return f"{prefix}github.com/{profile}" if profile else default

    @staticmethod
    def build_karma(
        user: BetterUser,
        data: BetterBotBaseDataService,
        chat_id: int = None
    ) -> str:
        """Builds the user's karma and returns its string representation.
        
        :param user: user object
        :param data: data service
        :param chat_id: chat ID for chat-specific karma, None for global display
        """
        plus_string = ""
        minus_string = ""
        
        if chat_id is not None:
            karma = data.get_user_chat_karma(user, chat_id)
            supporters = data.get_user_chat_supporters(user, chat_id)
            opponents = data.get_user_chat_opponents(user, chat_id)
        else:
            # Fallback to old behavior for backward compatibility
            karma_value = user["karma"]
            karma = karma_value if isinstance(karma_value, int) else 0
            supporters_value = user["supporters"]
            supporters = supporters_value if isinstance(supporters_value, list) else []
            opponents_value = user["opponents"]
            opponents = opponents_value if isinstance(opponents_value, list) else []
        
        up_votes = len(supporters)
        down_votes = len(opponents)
        
        if up_votes > 0:
            plus_string = "+%.1f" % (up_votes / config.POSITIVE_VOTES_PER_KARMA)
        if down_votes > 0:
            minus_string = "-%.1f" % (down_votes / config.NEGATIVE_VOTES_PER_KARMA)
        if up_votes > 0 or down_votes > 0:
            return f"[{karma}][{plus_string}{minus_string}]"
        else:
            return f"[{karma}]"

    @staticmethod
    def get_users_sorted_by_karma(
        vk_instance: Vk,
        data: BetterBotBaseDataService,
        peer_id: int,
        reverse_sort: bool = True
    ) -> List[BetterUser]:
        members = vk_instance.get_members_ids(peer_id)
        users = data.get_users(
            other_keys=[
                "karma", "name", "programming_languages",
                "supporters", "opponents", "github_profile", "uid"],
            sort_key=lambda u: DataBuilder.calculate_real_karma(u, data, peer_id),
            reverse_sort=reverse_sort)
        if members:
            users = [u for u in users if u["uid"] in members]
        return users

    @staticmethod
    def get_users_sorted_by_name(
        vk_instance,
        data: BetterBotBaseDataService,
        peer_id: int
    ) -> List[BetterUser]:
        members = vk_instance.get_members_ids(peer_id)
        users = data.get_users(
            other_keys=[
                "name", "programming_languages",
                "github_profile", "uid"
            ])
        if members:
            users = [u for u in users if u["uid"] in members]
        users.reverse()
        return users

    @staticmethod
    def calculate_real_karma(
        user: BetterUser,
        data: BetterBotBaseDataService,
        chat_id: int = None
    ) -> int:
        """Calculate real karma including pending votes.
        
        :param user: user object
        :param data: data service
        :param chat_id: chat ID for chat-specific karma calculation
        """
        if chat_id is not None:
            base_karma = data.get_user_chat_karma(user, chat_id)
            supporters = data.get_user_chat_supporters(user, chat_id)
            opponents = data.get_user_chat_opponents(user, chat_id)
        else:
            # Fallback to old behavior for backward compatibility
            karma_value = user["karma"]
            base_karma = karma_value if isinstance(karma_value, int) else 0
            supporters_value = user["supporters"]
            supporters = supporters_value if isinstance(supporters_value, list) else []
            opponents_value = user["opponents"]
            opponents = opponents_value if isinstance(opponents_value, list) else []
        
        up_votes = len(supporters) / config.POSITIVE_VOTES_PER_KARMA
        down_votes = len(opponents) / config.NEGATIVE_VOTES_PER_KARMA
        return base_karma + up_votes - down_votes
