INPUT_FILE="$1"
OUTPUT_FILE="$2"

code "$INPUT_FILE"

sleep 4

WINDOW="$INPUT_FILE - Visual Studio Code"
WINDOW_ID=$(xdotool search --name "$WINDOW")

xdotool windowactivate --sync $WINDOW_ID key --clearmodifiers ctrl+End key --clearmodifiers ctrl+Return &>/dev/null

sleep 7

xdotool windowactivate --sync $WINDOW_ID key --clearmodifiers ctrl+a &>/dev/null

sleep 1

xdotool windowactivate --sync $WINDOW_ID key --clearmodifiers ctrl+c &>/dev/null

sleep 1

xdotool windowactivate --sync $WINDOW_ID key --clearmodifiers ctrl+w &>/dev/null

sleep 1

xdotool windowactivate --sync $WINDOW_ID key --clearmodifiers ctrl+w &>/dev/null

sleep 1

code "$OUTPUT_FILE"

sleep 4

xdotool windowactivate --sync $WINDOW_ID key --clearmodifiers ctrl+a &>/dev/null

sleep 1

xdotool windowactivate --sync $WINDOW_ID key --clearmodifiers ctrl+v &>/dev/null

sleep 1

xdotool windowactivate --sync $WINDOW_ID key --clearmodifiers ctrl+s &>/dev/null

sleep 1

xdotool windowactivate --sync $WINDOW_ID key --clearmodifiers ctrl+w &>/dev/null