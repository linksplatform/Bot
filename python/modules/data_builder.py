# -*- coding: utf-8 -*-
from modules.data_service import BetterBotBaseDataService
from social_ethosa import BetterUser
from typing import List
import config


class DataBuilder:
    @staticmethod
    def build_programming_languages(
            user: BetterUser, data: BetterBotBaseDataService,
            default: str = "отсутствуют") -> str:
        """
        Builds the user's programming languages and returns its string representation.
        """
        languages = data.get_user_sorted_programming_languages(user)
        return ", ".join(languages) if len(languages) > 0 else default

    @staticmethod
    def build_github_profile(user: BetterUser, data: BetterBotBaseDataService,
                             default: str = "", prefix: str = "") -> str:
        """
        Builds the user's github profile and returns its string representation.
        """
        profile = data.get_user_property(user, "github_profile")
        return f"{prefix}github.com/{profile}" if profile else default

    @staticmethod
    def build_karma(user: BetterUser, data: BetterBotBaseDataService) -> str:
        """
        Builds the user's karma and returns its string representation.
        """
        plus_string = ""
        minus_string = ""
        karma = data.get_user_property(user, "karma")
        plus_votes = len(data.get_user_property(user, "supporters"))
        minus_votes = len(data.get_user_property(user, "opponents"))
        if plus_votes > 0:
            plus_string = "+%.1f" % (plus_votes / config.positive_votes_per_karma)
        if minus_votes > 0:
            minus_string = "-%.1f" % (minus_votes / config.negative_votes_per_karma)
        if plus_votes > 0 or minus_votes > 0:
            return f"[{karma}][{plus_string}{minus_string}]"
        else:
            return f"[{karma}]"

    @staticmethod
    def get_users_sorted_by_karma(
            vk_instance, data: BetterBotBaseDataService,
            peer_id: int, reverse_sort=True) -> List[BetterUser]:
        members = vk_instance.get_members_ids(peer_id)
        users = data.get_users(
            other_keys=["karma", "name", "programming_languages",
                        "supporters", "opponents", "github_profile", "uid"],
            sort_key=lambda u: DataBuilder.calculate_real_karma(u, data),
            reverse_sort=reverse_sort)
        if members:
            users = [u for u in users if u["uid"] in members]
        return users

    @staticmethod
    def get_users_sorted_by_name(
            vk_instance, data: BetterBotBaseDataService,
            peer_id: int) -> List[BetterUser]:
        members = vk_instance.get_members_ids(peer_id)
        users = data.get_users(other_keys=["name", "programming_languages", "github_profile", "uid"])
        if members:
            users = [u for u in users if u["uid"] in members]
        users.reverse()
        return users

    @staticmethod
    def calculate_real_karma(user: BetterUser, data: BetterBotBaseDataService) -> int:
        base_karma = data.get_user_property(user, "karma")
        up_votes = len(data.get_user_property(user, "supporters"))/config.positive_votes_per_karma
        down_votes = len(data.get_user_property(user, "opponents"))/config.negative_votes_per_karma
        return base_karma + up_votes - down_votes
