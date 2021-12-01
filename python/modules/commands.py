# -*- coding: utf-8 -*-
from modules.commands_builder import CommandsBuilder
from modules.data_service import BetterBotBaseDataService
from modules.data_builder import DataBuilder
from typing import NoReturn, Tuple, List
from regex import Pattern
from saya import Vk
import regex
import config


class Commands:
    def __init__(self, vk_instance: Vk, data_service: BetterBotBaseDataService):
        self.user = None
        self.msg: str = ""
        self.cmds: dict = {}
        self.peer_id: int = 0
        self.from_id: int = 0
        self.karma_enabled: bool = False
        self.is_bot_selected: bool = False
        self.fwd_messages: List[dict] = []
        self.selected_message: dict = {}
        self.vk_instance: Vk = vk_instance
        self.data_service: BetterBotBaseDataService = data_service

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
            matched: bool = regex.match(cmd, msg)
            if matched:
                action()
