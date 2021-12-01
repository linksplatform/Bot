# -*- coding: utf-8 -*-
from modules.commands_builder import CommandsBuilder
from modules.data_service import BetterBotBaseDataService
from modules.data_builder import DataBuilder
from social_ethosa import BetterUser
from typing import NoReturn, Tuple, List
from regex import Pattern
from saya import Vk
import requests
import config
import regex


class Commands:
    def __init__(self, vk_instance: Vk, data_service: BetterBotBaseDataService):
        self.cmds: dict = {}
        self.msg: str = ""
        self.peer_id: int = 0
        self.from_id: int = 0
        self.user: BetterUser = None
        self.karma_enabled: bool = False
        self.is_bot_selected: bool = False
        self.fwd_messages: List[dict] = []
        self.selected_message: dict = {}
        self.vk_instance: Vk = vk_instance
        self.data_service: BetterBotBaseDataService = data_service
        self.matched: list = []

    def help_message(self) -> NoReturn:
        """
        Sends help message
        """
        self.vk_instance.send_msg(
            CommandsBuilder.build_help_message(self.peer_id, self.karma_enabled),
            self.peer_id)

    def info_message(self) -> NoReturn:
        """
        Sends user info
        """
        self.vk_instance.send_msg(
            CommandsBuilder.build_info_message(self.user, self.data_service,
                                               self.from_id, self.karma_enabled),
            self.peer_id)

    def update_command(self) -> NoReturn:
        """
        Updates user profile.
        """
        name = self.vk_instance._get_user_name(self.from_id)
        self.data_service.set_user_property(self.user, "name", name)
        self.data_service.save_user(self.user)
        self.info_message()

    def change_programming_language(self, is_add: bool) -> NoReturn:
        """
        Adds or removes a new programming language in user profile.
        """
        language = self.matched.group('language')
        language = self.vk_instance._get_default_programming_language(language)
        if not language:
            return
        languages = self.data_service.get_user_property(self.user, "programming_languages")
        condition = language not in languages if is_add else language in languages
        if condition:
            if is_add:
                languages.append(language)
            else:
                languages.remove(language)
            self.data_service.set_user_property(self.user, "programming_languages", languages)
            self.data_service.save_user(self.user)
        self.vk_instance.send_msg(
            CommandsBuilder.build_change_programming_languages(self.user, self.data_service),
            self.peer_id)

    def change_github_profile(self, is_add: bool) -> NoReturn:
        """
        Changes github profile.
        """
        profile = self.matched.group('profile')
        if not profile:
            return
        user_profile = self.data_service.get_user_property(self.user, "github_profile")
        condition = profile != user_profile if is_add else profile == user_profile
        if not is_add:
            profile = ""
        if condition:
            if is_add and requests.get(f'https://github.com/{profile}').status_code != 200:
                return
            self.data_service.set_user_property(self.user, "github_profile", profile)
            self.data_service.save_user(self.user)
        self.vk_instance.send_msg(
            CommandsBuilder.build_github_profile(self.user, self.data_service),
            self.peer_id)

    def karma_message(self) -> NoReturn:
        """
        Shows user's karma.
        """
        if self.peer_id < 2e9 and not self.karma_enabled:
            return
        is_self = self.data_service.get_user_property(self.user, 'uid') == self.from_id
        self.vk_instance.send_msg(
            CommandsBuilder.build_karma(self.user, self.data_service, is_self),
            self.peer_id)

    def register_cmd(self, cmd: Pattern, action: callable) -> NoReturn:
        """
        Registers a new command.
        """
        self.cmds[cmd] = action

    def register_cmds(self, *cmds: Tuple[Pattern, callable]) -> NoReturn:
        """
        Registers a new commands.
        """
        for cmd, action in cmds:
            self.cmds[cmd] = action

    def process(self, msg: str, peer_id: int, from_id: int,
                fwd_messages: List[dict], user, selected_user) -> NoReturn:
        """
        Process commands

        Arguments:
        - {msg} - event message;
        - {peer_id} - chat ID;
        - {from_id} - user ID.
        """
        self.msg = msg
        self.from_id = from_id
        self.peer_id = peer_id
        self.karma_enabled = peer_id in config.chats_karma_whitelist
        self.fwd_messages = fwd_messages
        self.selected_message = fwd_messages[0] if len(fwd_messages) == 1 else None
        self.is_bot_selected = self.selected_message and (self.selected_message["from_id"] < 0)
        self.user = selected_user if selected_user else user

        for cmd, action in self.cmds.items():
            self.matched: list = regex.match(cmd, msg)
            if self.matched:
                action()
                return
