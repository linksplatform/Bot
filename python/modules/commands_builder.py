# -*- coding: utf-8 -*-
from modules.data_service import BetterBotBaseDataService
from modules.data_builder import DataBuilder
from social_ethosa import BetterUser


class CommandsBuilder:
    @staticmethod
    def build_help_message(peer_id: int, karma: bool) -> str:
        """
        Builds help message.

        Arguments:
        - {peer_id} - chat ID;
        - {karma} - is karma enabled in chat.
        """
        if 0 < peer_id < 2e9:
            return ("Вы находитесь в личных сообщениях бота.\n"
                    "Документация — vk.cc/auqYdx")
        elif peer_id > 2e9:
            if karma:
                return ("Вы находитесь в беседе с включённой кармой.\n"
                        "Документация — vk.cc/auqYdx")
            else:
                return (f"Вы находитесь в беседе (#{peer_id}) с выключенной кармой.\n"
                        "Документация — vk.cc/auqYdx")

    def build_info_message(user: BetterUser, data: BetterBotBaseDataService,
                           from_id: int, karma: bool) -> str:
        """
        Builds info message.

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
