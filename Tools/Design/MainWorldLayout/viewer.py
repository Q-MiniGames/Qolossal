"""Build the approved compact, label-free player map."""
from pathlib import Path
import runpy
runpy.run_path(str(Path(__file__).with_name("player_view.py")),run_name="__main__")
