# -*- coding: utf-8 -*-
"""Main Telegram bot implementation."""

import asyncio
import logging
import sys
import os

from aiogram import Bot, Dispatcher, Router, types
from aiogram.enums import ParseMode
from aiogram.filters import CommandStart, Command
from aiogram.types import Message
from aiogram.utils.markdown import hbold
from aiogram.webhook.aiohttp_server import SimpleRequestHandler, setup_application
from aiohttp import web

from modules.commands import Commands
import config

# Bot token (should be set in config.py)
TOKEN = config.BOT_TOKEN

# All handlers should be attached to the Router (or Dispatcher)
router = Router()
commands_handler = None

@router.message(CommandStart())
async def command_start_handler(message: Message) -> None:
    """Handle /start command."""
    await message.reply(
        f"Hello, {hbold(message.from_user.full_name)}!\n\n"
        f"I'm LinksBot, a bot for programmers! 🤖\n\n"
        f"Use /help to see available commands.",
        parse_mode=ParseMode.HTML
    )

@router.message(Command("help"))
async def help_command_handler(message: Message) -> None:
    """Handle /help command."""
    await commands_handler.help_command(message)

@router.message(Command("info"))
async def info_command_handler(message: Message) -> None:
    """Handle /info command."""
    await commands_handler.info_command(message)

@router.message(Command("update"))
async def update_command_handler(message: Message) -> None:
    """Handle /update command."""
    await commands_handler.update_command(message)

@router.message(Command("add_lang"))
async def add_lang_command_handler(message: Message) -> None:
    """Handle /add_lang command."""
    await commands_handler.add_language_command(message)

@router.message(Command("remove_lang"))
async def remove_lang_command_handler(message: Message) -> None:
    """Handle /remove_lang command."""
    await commands_handler.remove_language_command(message)

@router.message(Command("add_github"))
async def add_github_command_handler(message: Message) -> None:
    """Handle /add_github command."""
    await commands_handler.add_github_command(message)

@router.message(Command("remove_github"))
async def remove_github_command_handler(message: Message) -> None:
    """Handle /remove_github command."""
    await commands_handler.remove_github_command(message)

@router.message(Command("karma"))
async def karma_command_handler(message: Message) -> None:
    """Handle /karma command."""
    await commands_handler.karma_command(message)

@router.message(Command("top"))
async def top_command_handler(message: Message) -> None:
    """Handle /top command."""
    await commands_handler.top_command(message)

@router.message(Command("bottom"))
async def bottom_command_handler(message: Message) -> None:
    """Handle /bottom command."""
    await commands_handler.bottom_command(message)

@router.message(Command("people"))
async def people_command_handler(message: Message) -> None:
    """Handle /people command."""
    await commands_handler.people_command(message)

@router.message(Command("what_is"))
async def what_is_command_handler(message: Message) -> None:
    """Handle /what_is command."""
    await commands_handler.what_is_command(message)

@router.message()
async def message_handler(message: Message) -> None:
    """Handle all other messages."""
    text = message.text
    
    if not text:
        return
    
    text = text.strip()
    
    # Handle karma voting
    if text == "+" and message.reply_to_message:
        await commands_handler.vote_karma(message, positive=True)
    elif text == "-" and message.reply_to_message:
        await commands_handler.vote_karma(message, positive=False)
    elif text.startswith("+") and text[1:].isdigit() and message.reply_to_message:
        # Handle +N karma voting (future enhancement)
        await message.reply("Multiple karma voting not implemented yet.")
    elif text.startswith("-") and text[1:].isdigit() and message.reply_to_message:
        # Handle -N karma voting (future enhancement)
        await message.reply("Multiple karma voting not implemented yet.")


async def main() -> None:
    """Initialize and start the bot."""
    global commands_handler
    
    if not TOKEN:
        print("Error: BOT_TOKEN not set in config.py")
        sys.exit(1)
    
    # Initialize Bot instance with default parse mode
    bot = Bot(TOKEN, parse_mode=ParseMode.HTML)
    
    # Initialize commands handler
    commands_handler = Commands(bot)
    
    # And the run events dispatching
    dp = Dispatcher()
    dp.include_router(router)
    
    # Start polling
    await dp.start_polling(bot)


if __name__ == "__main__":
    # Set up logging
    logging.basicConfig(
        level=logging.INFO,
        format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
        stream=sys.stdout
    )
    
    # Create data directory
    os.makedirs('data', exist_ok=True)
    
    # Run the bot
    asyncio.run(main())