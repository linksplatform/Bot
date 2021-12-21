# -*- coding: utf-8 -*-
from modules.commands_builder import CommandsBuilder
from modules.data_service import BetterBotBaseDataService
from modules.data_builder import DataBuilder
from typing import NoReturn, Tuple, List
from datetime import datetime
from social_ethosa import BetterUser
from regex import Pattern, split, match
from saya import Vk
import requests
import config


class Commands:
    cmds: dict = {}
    def __init__(
        self,
        vk_instance: Vk,
        data_service: BetterBotBaseDataService
    ):
        self.msg: str = ""
        self.msg_id: int = 0
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
        """Sends help message
        """
        self.vk_instance.send_msg(
            CommandsBuilder.build_help_message(self.peer_id, self.karma_enabled),
            self.peer_id)

    def info_message(self) -> NoReturn:
        """Sends user info
        """
        self.vk_instance.send_msg(
            CommandsBuilder.build_info_message(
                self.user, self.data_service, self.from_id, self.karma_enabled),
            self.peer_id)

    def update_command(self) -> NoReturn:
        """Updates user profile.
        """
        name = self.vk_instance.get_user_name(self.from_id)
        self.data_service.set_user_property(self.current_user, "name", name)
        self.data_service.save_user(self.current_user)
        self.info_message()

    def change_programming_language(
        self,
        is_add: bool
    ) -> NoReturn:
        """Adds or removes a new programming language in user profile.
        """
        language = self.matched.group('language')
        language = self.vk_instance.get_default_programming_language(language).replace('\\', '')
        if not language:
            return
        languages = self.data_service.get_user_property(self.current_user, "programming_languages")
        condition = language not in languages if is_add else language in languages
        if condition:
            if is_add:
                languages.append(language)
            else:
                languages.remove(language)
            self.data_service.set_user_property(self.current_user, "programming_languages", languages)
            self.data_service.save_user(self.current_user)
        self.vk_instance.send_msg(
            CommandsBuilder.build_change_programming_languages(self.current_user, self.data_service),
            self.peer_id)

    def change_github_profile(
        self,
        is_add: bool
    ) -> NoReturn:
        """Changes github profile.
        """
        profile = self.matched.group('profile')
        if not profile:
            return
        user_profile = self.data_service.get_user_property(self.current_user, "github_profile")
        condition = profile != user_profile if is_add else profile == user_profile
        if not is_add:
            profile = ""
        if condition:
            if is_add and requests.get(f'https://github.com/{profile}').status_code != 200:
                return
            self.data_service.set_user_property(self.current_user, "github_profile", profile)
            self.data_service.save_user(self.current_user)
        self.vk_instance.send_msg(
            CommandsBuilder.build_github_profile(self.current_user, self.data_service),
            self.peer_id)

    def karma_message(self) -> NoReturn:
        """Shows user's karma.
        """
        if self.peer_id < 2e9 and not self.karma_enabled:
            return
        is_self = self.data_service.get_user_property(self.user, 'uid') == self.from_id
        self.vk_instance.send_msg(
            CommandsBuilder.build_karma(self.user, self.data_service, is_self),
            self.peer_id)

    def top(
        self,
        reverse: bool = False
    ) -> NoReturn:
        """Sends users top.
        """
        if self.peer_id < 2e9:
            return
        # maximum_users = self.matched.group("maximum_users")
        # maximum_users = int(maximum_users) if maximum_users else 0
        users = DataBuilder.get_users_sorted_by_karma(
            self.vk_instance, self.data_service, self.peer_id)
        users = [i for i in users if
                 (i["karma"] != 0) or ("programming_languages" in i and len(i["programming_languages"]) > 0)]
        self.vk_instance.send_msg(
            CommandsBuilder.build_top_users(users, self.data_service, reverse, self.karma_enabled),
            self.peer_id)

    def top_langs(
        self,
        reverse: bool = False
    ) -> NoReturn:
        """Sends users top.
        """
        if self.peer_id < 2e9:
            return
        languages = split(r"\s+", self.matched.group("languages"))
        users = DataBuilder.get_users_sorted_by_karma(
            self.vk_instance, self.data_service, self.peer_id, reverse)
        users = [i for i in users if
                 ("programming_languages" in i and len(i["programming_languages"]) > 0) and
                 self.vk_instance.contains_all_strings(i["programming_languages"], languages, True)]
        self.vk_instance.send_msg(
            CommandsBuilder.build_top_users(users, self.data_service, reverse, self.karma_enabled),
            self.peer_id)

    def apply_karma(self) -> NoReturn:
        """Changes user karma.
        """
        if self.peer_id < 2e9 or not self.karma_enabled or not self.matched or self.is_bot_selected:
            return
        if not self.user:
            selected_user_id = self.matched.group("selectedUserId")
            if selected_user_id:
                self.user = self.data_service.get_or_create_user(int(selected_user_id), self)

        if self.user and (self.data_service.get_user_property(self.user, "uid") != self.from_id):
            operator = self.matched.group("operator")[0]
            amount = self.matched.group("amount")
            amount = int(amount) if amount else 0

            utcnow = datetime.utcnow()

            # Downvotes disabled for users with negative karma
            if (operator == "-") and (self.data_service.get_user_property(self.current_user, "karma") < 0):
                self.vk_instance.delete_message(self.peer_id, self.msg_id)
                self.vk_instance.send_msg(
                    CommandsBuilder.build_not_enough_karma(self.current_user, self.data_service),
                    self.peer_id)
                return

            # Collective votes limit
            if amount == 0:
                current_voters = "supporters" if operator == "+" else "opponents"
                if self.current_user.uid in self.user[current_voters]:
                    self.vk_instance.send_msg(
                        (f'Вы уже голосовали за [id{self.user.uid}|'
                         f'{self.vk_instance.get_user_name(self.user.uid, "acc")}].'),
                        self.peer_id
                    )
                    return
                utclast = datetime.fromtimestamp(
                    float(self.data_service.get_user_property(
                        self.current_user, "last_collective_vote")
                ))
                difference = utcnow - utclast
                hours_difference = difference.total_seconds() / 3600
                hours_limit = self.vk_instance.karma_limit(
                    self.data_service.get_user_property(self.current_user, "karma"))
                if hours_difference < hours_limit:
                    self.vk_instance.delete_message(self.peer_id, self.msg_id)
                    self.vk_instance.send_msg(
                        CommandsBuilder.build_not_enough_hours(
                            self.current_user, self.data_service,
                            hours_limit, difference.total_seconds() / 60),
                        self.peer_id)
                    return

            user_karma_change, selected_user_karma_change, collective_vote_applied, voters = self.apply_karma_change(operator, amount)

            if collective_vote_applied:
                self.data_service.set_user_property(self.current_user, "last_collective_vote", int(utcnow.timestamp()))
                self.data_service.save_user(self.user)

            self.data_service.save_user(self.current_user)

            if user_karma_change:
                self.data_service.save_user(self.user)
            self.vk_instance.send_msg(
                CommandsBuilder.build_karma_change(
                    user_karma_change, selected_user_karma_change, voters),
                self.peer_id)
            self.vk_instance.delete_message(self.peer_id, self.msg_id)

    def apply_karma_change(
        self,
        operator: str,
        amount: int
    ) -> tuple:
        selected_user_karma_change = None
        user_karma_change = None
        collective_vote_applied = None
        voters = None

        # Personal karma transfer
        if amount > 0:
            if self.data_service.get_user_property(self.current_user, "karma") < amount:
                self.vk_instance.send_msg(
                    CommandsBuilder.build_not_enough_karma(self.current_user, self.data_service),
                    self.peer_id)
                return user_karma_change, selected_user_karma_change, collective_vote_applied, voters
            else:
                user_karma_change = self.apply_user_karma(self.current_user, -amount)
                amount = -amount if operator == "-" else amount
                selected_user_karma_change = self.apply_user_karma(self.user, amount)

        # Collective vote
        elif amount == 0:
            if operator == "+":
                selected_user_karma_change, voters, collective_vote_applied = self.apply_collective_vote("supporters", config.POSITIVE_VOTES_PER_KARMA, +1)
            else:
                selected_user_karma_change, voters, collective_vote_applied = self.apply_collective_vote("opponents", config.NEGATIVE_VOTES_PER_KARMA, -1)

        return user_karma_change, selected_user_karma_change, collective_vote_applied, voters

    def apply_collective_vote(
        self,
        current_voters: str,
        number_of_voters: int,
        amount: int
    ) -> tuple:
        vote_applied = None
        if self.current_user.uid not in self.user[current_voters]:
            self.user[current_voters].append(self.current_user.uid)
            vote_applied = True
        if len(self.user[current_voters]) >= number_of_voters:
            voters = self.user[current_voters]
            self.user[current_voters] = []
            return self.apply_user_karma(self.user, amount), voters, vote_applied
        return (None, None, vote_applied)

    def apply_user_karma(self, user: BetterUser, amount: int) -> Tuple[int, str, int, int]:
        initial_karma = self.data_service.get_user_property(user, "karma")
        new_karma = initial_karma + amount
        self.data_service.set_user_property(user, "karma", new_karma)
        return (user.uid,
                self.data_service.get_user_property(user, "name"),
                initial_karma,
                new_karma)

    @staticmethod
    def register_cmd(
        cmd: Pattern,
        action: callable
    ) -> NoReturn:
        """Registers a new command.
        """
        Commands.cmds[cmd] = action

    @staticmethod
    def register_cmds(
        *cmds: Tuple[Pattern, callable]
    ) -> NoReturn:
        """Registers a new commands.
        """
        for cmd, action in cmds:
            Commands.cmds[cmd] = action

    @staticmethod
    def _register_decorator(
        cmd: Pattern,
        args: list,
        action: callable
    ) -> NoReturn:
        Commands.cmds[cmd] = lambda: action(args)

    @staticmethod
    def register(
        cmd: Pattern,
        *args
    ) -> callable:
        """Command register decorator.
        """
        return lambda action: Commands._register_decorator(cmd, args, action)

    def process(
        self,
        msg: str,
        peer_id: int,
        from_id: int,
        fwd_messages: List[dict],
        msg_id: int,
        user: BetterUser,
        selected_user: BetterUser
    ) -> NoReturn:
        """Process commands

        Arguments:
        - {msg} - event message;
        - {peer_id} - chat ID;
        - {from_id} - user ID.
        """
        self.msg = msg
        self.msg_id = msg_id
        self.from_id = from_id
        self.peer_id = peer_id
        self.karma_enabled = peer_id in config.CHATS_KARMA_WHITELIST
        self.fwd_messages = fwd_messages
        self.selected_message = fwd_messages[0] if len(fwd_messages) == 1 else None
        self.is_bot_selected = self.selected_message and (self.selected_message["from_id"] < 0)
        self.user = selected_user if selected_user else user
        self.current_user = user

        if from_id < 0:
            return

        for cmd, action in Commands.cmds.items():
            self.matched: list = match(cmd, msg)
            if self.matched:
                action()
                return
