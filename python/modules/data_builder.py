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
        data: BetterBotBaseDataService
    ) -> str:
        """Builds the user's karma and returns its string representation.
        """
        plus_string = ""
        minus_string = ""
        karma = user["karma"]
        up_votes = len(user["supporters"])
        down_votes = len(user["opponents"])
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
            sort_key=lambda u: DataBuilder.calculate_real_karma(u, data),
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
        data: BetterBotBaseDataService
    ) -> int:
        base_karma = user["karma"]
        up_votes = len(user["supporters"])/config.POSITIVE_VOTES_PER_KARMA
        down_votes = len(user["opponents"])/config.NEGATIVE_VOTES_PER_KARMA
        return base_karma + up_votes - down_votes

    @staticmethod
    def build_local_karma(
        user: BetterUser,
        data: BetterBotBaseDataService,
        chat_id: int
    ) -> str:
        """Builds the user's local karma for specific chat and returns its string representation.
        """
        local_karma = data.get_local_karma(user, chat_id)
        plus_string = ""
        minus_string = ""
        up_votes = len(user["supporters"])
        down_votes = len(user["opponents"])
        if up_votes > 0:
            plus_string = "+%.1f" % (up_votes / config.POSITIVE_VOTES_PER_KARMA)
        if down_votes > 0:
            minus_string = "-%.1f" % (down_votes / config.NEGATIVE_VOTES_PER_KARMA)
        if up_votes > 0 or down_votes > 0:
            return f"[{local_karma}][{plus_string}{minus_string}]"
        else:
            return f"[{local_karma}]"

    @staticmethod
    def get_users_sorted_by_local_karma(
        vk_instance: Vk,
        data: BetterBotBaseDataService,
        peer_id: int,
        chat_id: int
    ) -> List[Dict[str, Any]]:
        """Returns users from the chat sorted by local karma."""
        members = vk_instance.get_members_ids(peer_id)
        users = []
        for member_id in members:
            user = data.get_user(member_id)
            local_karma = data.get_local_karma(user, chat_id)
            users.append({
                "uid": member_id,
                "local_karma": local_karma,
                "name": user.name,
                "programming_languages": user.programming_languages,
                "github_profile": user.github_profile
            })
        return sorted(users, key=lambda u: u["local_karma"], reverse=True)

    @staticmethod
    def build_programming_languages_from_dict(
        user_dict: Dict[str, Any],
        default: str = "отсутствуют"
    ) -> str:
        """Builds programming languages from user dictionary."""
        languages = user_dict.get("programming_languages", [])
        languages = languages if isinstance(languages, list) else []
        return ", ".join(sorted(languages)) if len(languages) > 0 else default

    @staticmethod
    def build_github_profile_from_dict(
        user_dict: Dict[str, Any],
        prefix: str = ""
    ) -> str:
        """Builds github profile from user dictionary."""
        profile = user_dict.get("github_profile", "")
        return f"{prefix}github.com/{profile}" if profile else ""
