# -*- coding: utf-8 -*-


class CommandsBuilder:
    @staticmethod
    def build_help_message(peer_id: int, karma: bool) -> str:
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

