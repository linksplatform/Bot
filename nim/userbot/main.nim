import
  asyncdispatch,
  parsecfg,  # working with config files
  shizuka  # working with VK API


let
  cfg = loadConfig("user.cfg")
  token = cfg.getSectionValue("user_setting", "api_token")
  vk = newVk(token)


proc addAllFriends {.async.} =
  let response = await vk~friends.getRequests(need_viewed=true)
  if response.hasKey("response"):
    for i in response["response"]["items"].items():
      echo await vk~friends.add(user_id=i)


waitFor addAllFriends()
