"""Local stdio MCP server for Jarvis Launcher."""

import shutil
import subprocess
import sys
from pathlib import Path
from typing import Literal

from mcp.server.fastmcp import FastMCP
from mcp.types import ToolAnnotations

ROOT = Path(__file__).resolve().parent
LAUNCHER = ROOT / "plugins" / "jarvis-console"
ASSETS = LAUNCHER / "assets"
server = FastMCP("Jarvis Launcher")


@server.tool(annotations=ToolAnnotations(readOnlyHint=True, openWorldHint=False))
def jarvis_status() -> dict[str, bool | str]:
    """Report which local Jarvis features are available. Does not open a window."""
    return {
        "interactive_windows_launcher": sys.platform == "win32" and (LAUNCHER / "Jarvis.exe").is_file(),
        "animation": (ASSETS / "boot.mp4").is_file(),
        "editing_kit": (ASSETS / "edit-kit.zip").is_file(),
        "platform": sys.platform,
    }


@server.tool(annotations=ToolAnnotations(destructiveHint=False, openWorldHint=False))
def jarvis_launch(mode: Literal["boot", "console", "awake"] = "boot") -> dict[str, bool | str | int]:
    """Open Jarvis on the user's own desktop when explicitly requested. On macOS and Linux, open the animation only."""
    movie = ASSETS / "boot.mp4"
    if not movie.is_file():
        raise FileNotFoundError("Missing animation assets. Download the complete Jarvis Launcher package.")
    if sys.platform == "win32":
        exe = LAUNCHER / "Jarvis.exe"
        if not exe.is_file():
            raise FileNotFoundError("Run build.ps1 before launching Jarvis on Windows.")
        args = [str(exe), "--return-to-client"]
        if mode != "boot":
            args.append("--" + mode)
    elif mode == "boot":
        opener = "open" if sys.platform == "darwin" else "xdg-open"
        command = shutil.which(opener)
        if not command:
            raise RuntimeError("A desktop file opener is required to play the animation.")
        args = [command, str(movie)]
    else:
        raise RuntimeError("The interactive console is available on Windows.")
    process = subprocess.Popen(
        args, cwd=LAUNCHER, stdin=subprocess.DEVNULL,
        stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
    )
    return {"started": True, "mode": mode, "pid": process.pid}


@server.tool(annotations=ToolAnnotations(readOnlyHint=True, openWorldHint=False))
def jarvis_editing_assets() -> dict[str, str | int]:
    """Return the location of the bundled timeline, audio tracks, subtitles and source assets."""
    archive = ASSETS / "edit-kit.zip"
    if not archive.is_file():
        raise FileNotFoundError("Download the complete Jarvis Launcher package to get the editing kit.")
    return {"archive": str(archive), "bytes": archive.stat().st_size}


if __name__ == "__main__":
    server.run(transport="stdio")
