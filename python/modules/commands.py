# -*- coding: utf-8 -*-
from typing import NoReturn, Tuple, List, Dict, Any, Callable, Optional
from datetime import datetime
from time import time
import os

from regex import Pattern, Match, split, match, search, IGNORECASE, sub
from requests import post
from social_ethosa import BetterUser
from saya import Vk
import wikipedia

from .commands_builder import CommandsBuilder
from .data_service import BetterBotBaseDataService
from .data_builder import DataBuilder
from .utils import (
    get_default_programming_language,
    contains_all_strings,
    karma_limit,
    is_available_ghpage
)
import config
import tokens


class Commands:
    cmds: Dict[Pattern, Any] = {}

    def __init__(
            self,
            vk_instance: Vk,
            data_service: BetterBotBaseDataService
    ):
        self.msg: str = ""
        self.msg_id: int = 0
        self.peer_id: int = 0
        self.from_id: int = 0
        self.now: float = time() - config.GITHUB_COPILOT_TIMEOUT
        self.user: BetterUser = None
        self.karma_enabled: bool = False
        self.is_bot_selected: bool = False
        self.fwd_messages: List[Dict[str, Any]] = []
        self.selected_message: Dict[str, Any] = {}
        self.vk_instance: Vk = vk_instance
        self.data_service: BetterBotBaseDataService = data_service
        self.matched: Match = None
        wikipedia.set_lang('en')

    def help_message(self) -> NoReturn:
        """Sends help message"""
        self.vk_instance.send_msg(
            CommandsBuilder.build_help_message(self.peer_id, self.karma_enabled),
            self.peer_id)

    def info_message(self) -> NoReturn:
        """Sends user info"""
        self.vk_instance.send_msg(
            CommandsBuilder.build_info_message(
                self.user, self.data_service, self.from_id, self.karma_enabled),
            self.peer_id)

    def update_command(self) -> NoReturn:
        """Updates user profile."""
        if self.from_id > 0:
            name = self.vk_instance.get_user_name(self.from_id)
            self.current_user.name = name
            self.data_service.save_user(self.current_user)
            self.info_message()

    def change_programming_language(
            self,
            is_add: bool
    ) -> NoReturn:
        """Adds or removes a new programming language in user profile."""
        language = self.matched.group('language')
        language = get_default_programming_language(language).replace('\\', '')
        if not language:
            return
        languages = self.current_user.programming_languages
        condition = language not in languages if is_add else language in languages
        if condition:
            if is_add:
                languages.append(language)
            else:
                languages.remove(language)
            self.current_user.programming_languages = languages
            self.data_service.save_user(self.current_user)
        self.vk_instance.send_msg(
            CommandsBuilder.build_change_programming_languages(
                self.current_user, self.data_service),
            self.peer_id)

    def change_github_profile(
            self,
            is_add: bool
    ) -> NoReturn:
        """Changes github profile."""
        profile = self.matched.group('profile')
        if not profile:
            return
        user_profile = self.current_user.github_profile
        condition = profile != user_profile if is_add else profile == user_profile
        if not is_add:
            profile = ""
        if condition:
            if is_add and not is_available_ghpage(profile):
                return
            self.current_user.github_profile = profile
            self.data_service.save_user(self.current_user)
        self.vk_instance.send_msg(
            CommandsBuilder.build_github_profile(self.current_user, self.data_service),
            self.peer_id)

    def karma_message(self) -> NoReturn:
        """Shows user's karma."""
        if self.peer_id < 2e9 and not self.karma_enabled:
            return
        is_self = self.user.uid == self.from_id
        self.vk_instance.send_msg(
            CommandsBuilder.build_karma(self.user, self.data_service, is_self),
            self.peer_id)

    def top(
            self,
            reverse: bool = False
    ) -> NoReturn:
        """Sends users top."""
        if self.peer_id < 2e9:
            return
        maximum_users = self.matched.group("maximum_users")
        maximum_users = int(maximum_users) if maximum_users else -1
        users = DataBuilder.get_users_sorted_by_karma(
            self.vk_instance, self.data_service, self.peer_id)
        users = [i for i in users if
                 (i["karma"] != 0) or
                 ("programming_languages" in i and len(i["programming_languages"]) > 0)
                 ]
        self.vk_instance.send_msg(
            CommandsBuilder.build_top_users(
                users, self.data_service, reverse,
                self.karma_enabled, maximum_users),
            self.peer_id)

    def top_langs(
            self,
            reverse: bool = False
    ) -> NoReturn:
        """Sends users top."""
        if self.peer_id < 2e9:
            return
        languages = split(r"\s+", self.matched.group("languages"))
        count = self.matched.group("count")
        users = DataBuilder.get_users_sorted_by_karma(
            self.vk_instance, self.data_service, self.peer_id)
        users = [i for i in users if
                 ("programming_languages" in i and len(i["programming_languages"]) > 0) and
                 contains_all_strings(i["programming_languages"], languages, True)]
        built = CommandsBuilder.build_top_users(
            users[:int(count.strip())] if count else users, self.data_service, reverse, self.karma_enabled)
        if built:
            self.vk_instance.send_msg(built, self.peer_id)
            return

    def apply_karma(self) -> NoReturn:
        """Changes user karma."""
        if self.peer_id < 2e9 or not self.karma_enabled or not self.matched or self.is_bot_selected:
            return
        if not self.user:
            selected_user_id = self.matched.group("selectedUserId")
            if selected_user_id:
                self.user = self.data_service.get_user(int(selected_user_id), self)

        if self.user and (self.user.uid != self.from_id):
            operator = self.matched.group("operator")[0]
            amount = self.matched.group("amount")
            amount = int(amount) if amount else 0

            utcnow = datetime.utcnow()

            # Downvotes disabled for users with negative karma
            if operator == "-" and self.current_user.karma < 0:
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
                    float(self.current_user.last_collective_vote))
                difference = utcnow - utclast
                hours_difference = difference.total_seconds() / 3600
                hours_limit = karma_limit(
                    self.current_user.karma)
                if hours_difference < hours_limit:
                    self.vk_instance.delete_message(self.peer_id, self.msg_id)
                    self.vk_instance.send_msg(
                        CommandsBuilder.build_not_enough_hours(
                            self.current_user, self.data_service,
                            hours_limit, difference.total_seconds() / 60),
                        self.peer_id)
                    return

            user_karma_change, selected_user_karma_change, collective_vote_applied, voters = self.apply_karma_change(
                operator, amount)

            if collective_vote_applied:
                self.current_user.last_collective_vote = int(utcnow.timestamp())
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
    ) -> Tuple[
        Optional[Tuple[int, str, int, int]],  # current user karma changed
        Optional[Tuple[int, str, int, int]],  # selected user karma changed
        Optional[bool],  # collective vote applied
        List[int]  # voters IDs list
    ]:
        selected_user_karma_change = None
        user_karma_change = None
        collective_vote_applied = None
        voters = None

        # Personal karma transfer
        if amount > 0:
            if self.current_user.karma < amount:
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
                selected_user_karma_change, voters, collective_vote_applied = self.apply_collective_vote("supporters",
                                                                                                         config.POSITIVE_VOTES_PER_KARMA,
                                                                                                         +1)
            else:
                selected_user_karma_change, voters, collective_vote_applied = self.apply_collective_vote("opponents",
                                                                                                         config.NEGATIVE_VOTES_PER_KARMA,
                                                                                                         -1)

        return user_karma_change, selected_user_karma_change, collective_vote_applied, voters

    def apply_collective_vote(
            self,
            current_voters: str,
            number_of_voters: int,
            amount: int
    ) -> Tuple[
        Optional[Tuple[int, str, int, int]],  # user ID, username, init karma, new karma
        Optional[List[int]],  # voters
        bool  # vote applied
    ]:
        """Applies collective vote

        :param current_voters: must be 'opponents' or 'supporters'
        :param number_of_voters: maximum voters for voters type.
        :param amount: positive or negative number.
        """
        vote_applied = False
        if self.current_user.uid not in self.user[current_voters]:
            self.user[current_voters].append(self.current_user.uid)
            vote_applied = True
        if len(self.user[current_voters]) >= number_of_voters:
            voters = self.user[current_voters]
            self.user[current_voters] = []
            return self.apply_user_karma(self.user, amount), voters, vote_applied
        return None, None, vote_applied

    @staticmethod
    def apply_user_karma(
            user: BetterUser,
            amount: int
    ) -> Tuple[int, str, int, int]:
        """Changes user karma

        :param user: user object
        :param amount: karma amount to change
        """
        initial_karma = user.karma
        new_karma = initial_karma + amount
        user.karma = new_karma
        return (user.uid, user.name, initial_karma, new_karma)

    def what_is(self) -> NoReturn:
        """Search on wikipedia and sends if available"""
        question = self.matched.groups()
        question = question[1] if question[1] else question[2]
        if search(r'[а-яё]+', question, IGNORECASE):
            wikipedia.set_lang('ru')
        else:
            wikipedia.set_lang('en')
        results = wikipedia.search(question)
        if results:
            try:
                page = wikipedia.page(results[0])
                self.vk_instance.send_msg(
                    sub(r"\\s{2,}", " ", page.summary[:256]) + f'...\n\n{page.url[8:]}', self.peer_id)
            except wikipedia.exceptions.DisambiguationError as e:
                # Select random page from references page and sends it
                results = wikipedia.search(question, suggestion=True)
                links = "\n".join([f"ru.wikipedia.org/wiki/{i.replace(' ', '_')}" for i in results[0][1:]])
                self.vk_instance.send_msg(
                    sub(r"\\s{2,}", " ",
                    f'ru.wikipedia.org/wiki/{results[0][0]}\n\nЕще на тему:\n{links}'), self.peer_id)

    def github_copilot(self) -> NoReturn:
        """send user task to GitHub Copilot"""
        now = time()
        if now - self.now >= config.GITHUB_COPILOT_TIMEOUT:
            self.now = now
            language = self.matched.group('lang')
            text = self.matched.group('text').strip()
            # input-output files
            input_file = f'input{config.GITHUB_COPILOT_LANGUAGES[language][0]}'
            output_file = f'output{config.GITHUB_COPILOT_LANGUAGES[language][0]}'
            with open(input_file, 'w', encoding='utf-8') as f:
                f.write(text)
            # run.sh input.py
            command = config.GITHUB_COPILOT_RUN_COMMAND.format(
                input_file=input_file, output_file=output_file)
            os.system(command)
            with open(output_file, 'r', encoding='utf-8') as f:
                result = f.read()
                if 'Synthesizing 0/10 solutions' in result or not result:
                    self.vk_instance.send_msg(
                        'Не удалось сгенерировать код.', self.peer_id
                    )
                    return
                response = post(
                    'https://pastebin.com/api/api_post.php',
                    data={
                        'api_dev_key': tokens.PASTEBIN_API_KEY,
                        'api_paste_code': result,
                        'api_paste_private': '0',
                        'api_paste_name': '',
                        'api_paste_expire_date': 'N',
                        'api_user_key': '',
                        'api_paste_format': config.GITHUB_COPILOT_LANGUAGES[language][1],
                        'api_option': 'paste'
                    }
                )
                # send pastebin URL
                if 'Post limit' in response.text:
                    self.vk_instance.send_msg(result.replace(' ', ' '), self.peer_id)
                else:
                    self.vk_instance.send_msg(
                        result.replace(' ', ' ') + '\n\nСгенерированный код: ' + response.text[8:],
                        self.peer_id
                    )
            return
        self.vk_instance.send_msg(
            f'Пожалуйста, подождите {round(config.GITHUB_COPILOT_TIMEOUT - (now - self.now))} секунд', self.peer_id
        )

    def match_command(
            self,
            pattern: Pattern
    ) -> NoReturn:
        self.matched = match(pattern, self.msg)

    @staticmethod
    def register_cmd(
            cmd: Pattern,
            action: Callable[[], NoReturn]
    ) -> NoReturn:
        """Registers a new command."""
        Commands.cmds[cmd] = action

    @staticmethod
    def register_cmds(
            *cmds: Tuple[Pattern, Callable[[], NoReturn]]
    ) -> NoReturn:
        """Registers a new commands."""
        for cmd, action in cmds:
            Commands.cmds[cmd] = action

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

        :param msg: event message
        :param peer_id: chat ID
        :param from_id: user ID
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
            self.match_command(cmd)
            if self.matched:
                action()
                return
