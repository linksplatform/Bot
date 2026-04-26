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
        karma = user.get("karma", 0)
        return str(karma)