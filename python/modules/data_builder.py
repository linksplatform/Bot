# -*- coding: utf-8 -*-
from modules.data_service import BetterBotBaseDataService
from social_ethosa import BetterUser
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
