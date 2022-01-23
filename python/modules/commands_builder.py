# -*- coding: utf-8 -*-
from typing import List, Optional, Tuple

from social_ethosa import BetterUser

from .data_service import BetterBotBaseDataService
from .data_builder import DataBuilder


class CommandsBuilder:
    @staticmethod
    def build_help_message(
        peer_id: int,
        karma: bool
    ) -> str:
        """Builds help message.

        Arguments:
        - {peer_id} - chat ID;
        - {karma} - is karma enabled in chat.
        """
        documentation_link = "vk.cc/c9TNs3"
        if 0 < peer_id < 2e9:
            return ("Вы находитесь в личных сообщениях бота.\n"
                    f"Документация — {documentation_link}")
        elif peer_id > 2e9:
            if karma:
                return ("Вы находитесь в беседе с включённой кармой.\n"
                        f"Документация — {documentation_link}")
            else:
                return (f"Вы находитесь в беседе (#{peer_id}) с выключенной кармой.\n"
                        f"Документация — {documentation_link}")

    @staticmethod
    def build_info_message(
        user: BetterUser,
        data: BetterBotBaseDataService,
        from_id: int,
        karma: bool
    ) -> str:
        """Builds info message.

        Arguments:
        - {user} - selected user;
        - {data} - data service;
        - {peer_id} - chat ID;
        - {karma} - is karma enabled in chat.
        """
        programming_languages_string = DataBuilder.build_programming_languages(user, data)
        profile = DataBuilder.build_github_profile(user, data, default="отсутствует")
        mention = f"[id{data.get_user_property(user, 'uid')}|{data.get_user_property(user, 'name')}]"
        is_self = data.get_user_property(user, 'uid') == from_id
        karma_str: str = ""
        if karma:
            if is_self:
                karma_str = f"{mention}, Ваша карма - {DataBuilder.build_karma(user, data)}.\n"
            else:
                karma_str = f"Карма {mention} - {DataBuilder.build_karma(user, data)}.\n"
        else:
            karma_str = f"{mention}.\n"
        return (f"{karma_str}"
                f"Языки программирования: {programming_languages_string}\n"
                f"Страничка на GitHub: {profile}.")

    @staticmethod
    def build_change_programming_languages(
        user: BetterUser,
        data: BetterBotBaseDataService
    ) -> str:
        """Builds changing programming languages.
        """
        programming_languages_string = DataBuilder.build_programming_languages(user, data)
        if not programming_languages_string:
            return (f"[id{data.get_user_property(user, 'uid')}|{data.get_user_property(user, 'name')}]"
                    f", у Вас не указано языков программирования.")
        else:
            return (f"[id{data.get_user_property(user, 'uid')}|{data.get_user_property(user, 'name')}]"
                    f", Ваши языки программирования: {programming_languages_string}.")

    @staticmethod
    def build_github_profile(
        user: BetterUser,
        data: BetterBotBaseDataService
    ) -> str:
        """Builds changing github profile.
        """
        profile = DataBuilder.build_github_profile(user, data, default="отсутствует")
        if not profile:
            return (f"[id{data.get_user_property(user, 'uid')}|{data.get_user_property(user, 'name')}], "
                    f"у Вас не указана страничка на GitHub.")
        else:
            return (f"[id{data.get_user_property(user, 'uid')}|{data.get_user_property(user, 'name')}], "
                    f"Ваша страничка на GitHub — {profile}.")

    @staticmethod
    def build_karma(
        user: BetterUser,
        data: BetterBotBaseDataService,
        is_self: bool
    ) -> str:
        """Sends user karma amount.
        """
        if is_self:
            return (f"[id{data.get_user_property(user, 'uid')}|"
                        f"{data.get_user_property(user, 'name')}], "
                        f"Ваша карма — {DataBuilder.build_karma(user, data)}.")
        else:
            return (f"Карма [id{data.get_user_property(user, 'uid')}|"
                        f"{data.get_user_property(user, 'name')}] — "
                        f"{DataBuilder.build_karma(user, data)}.")

    @staticmethod
    def build_not_enough_karma(
        user: BetterUser,
        data: BetterBotBaseDataService
    ) -> str:
        return (f"Извините, [id{data.get_user_property(user, 'uid')}|"
                f"{data.get_user_property(user, 'name')}], "
                f"но Вашей кармы [{data.get_user_property(user, 'karma')}] "
                f"недостаточно :(")

    @staticmethod
    def build_not_in_whitelist(
        user: BetterUser,
        data: BetterBotBaseDataService,
        peer_id: int
    ) -> str:
        return (f"Извините, [id{data.get_user_property(user, 'uid')}|"
                f"{data.get_user_property(user, 'name')}], "
                f"но Ваша беседа [{peer_id}] отсутствует в белом списке для начисления кармы.")

    @staticmethod
    def build_not_enough_hours(
        user: BetterUser,
        data: BetterBotBaseDataService,
        hours_limit: int,
        difference_minutes: int
    ) -> str:
        return (f"Извините, [id{data.get_user_property(user, 'uid')}|"
                f"{data.get_user_property(user, 'name')}], "
                f"но с момента вашего последнего голоса ещё не прошло {hours_limit} ч. "
                f":( До следующего голоса осталось {int(hours_limit * 60 - difference_minutes)} м.")

    @staticmethod
    def build_top_users(
        users: List[BetterUser],
        data: BetterBotBaseDataService,
        reverse: bool = False,
        has_karma: bool = True,
        maximum_users: int = -1
    ) -> Optional[str]:
        if not users:
            return None
        if reverse:
            users = reversed(users)
        user_strings = [(f"{DataBuilder.build_karma(user, data) if has_karma else ''} "
                         f"[id{data.get_user_property(user, 'uid')}|{data.get_user_property(user, 'name')}]"
                         f"{DataBuilder.build_github_profile(user, data, prefix=' - ')} "
                         f"{DataBuilder.build_programming_languages(user, data, '')}") for user in users]
        total_symbols = 0
        i = 0
        for user_string in user_strings:
            user_string_length = len(user_string)
            if (total_symbols + user_string_length + 2) >= 4096:  # Maximum message size for VK API (messages.send)
                user_strings = user_strings[:i]
            else:
                total_symbols += user_string_length + 2
                i += 1
        if maximum_users > 0:
            return '\n'.join(user_strings[:maximum_users])
        return '\n'.join(user_strings)

    @staticmethod
    def build_karma_change(
        user_karma_change: Optional[Tuple[int, str, int, int]],
        selected_user_karma_change: Optional[Tuple[int, str, int, int]],
        voters: List[int]
    ) -> Optional[str]:
        """Builds karma changing
        """
        if selected_user_karma_change:
            if user_karma_change:
                return ("Карма изменена: [id%s|%s] [%s]->[%s], [id%s|%s] [%s]->[%s]." %
                        (user_karma_change + selected_user_karma_change))
            return ("Карма изменена: [id%s|%s] [%s]->[%s]. Голосовали: (%s)" %
                (selected_user_karma_change + (", ".join([f"@id{voter}" for voter in voters]),)))
        return None
