# Bot

## Prerequisites
* [Git](https://git-scm.com/downloads)
* [Python 3](https://www.python.org/downloads)

## Install
```
git clone https://github.com/linksplatform/Bot.git
cd Bot
pip3 install -r requirements.txt
```

## Update

```
git pull
pip3 install -r requirements.txt --upgrade
```

## Configure

0. Enable VK Long Poll API with `5.103` version for your bot's group. ![Screenshot_20210630_110401](https://user-images.githubusercontent.com/1431904/123924747-eed01900-d992-11eb-8f8e-cf66b398ed90.png)
1. Set bot group id in [config.py](https://github.com/linksplatform/Bot/blob/e10f51c7e3711c43708ce5659c7de9e76cab6702/python/config.py#L3-L4).
2. Add tokens into `python/tokens.py` file
    * `BotToken` (an access token of your VK group). This token should have two access settings `community management` and `community messages`. This token is **required**. ![Screenshot_20210630_110724](https://user-images.githubusercontent.com/1431904/123925211-5d14db80-d993-11eb-8e79-9cb0ac49d1c6.png)
    * `UserToken` (an access token to your VK user via KateMobile). This token is used to delete messages in the chats where your user is administrator. This token is **optional**.

## Run

```Shell
cd python
python3 __main__.py
```

## Do not upload tokens.py with your real tokens please

To tell git to ignore this file:

```
git update-index --assume-unchanged python/tokens.py
```

## See the bot in action

To see the bot in action you can [join](https://vk.me/join/AJQ1d9E/bxbPjY87MeKsXgMa) the chat with this bot.
