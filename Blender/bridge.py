from __future__ import annotations

import json
import os
import struct
import subprocess
import tempfile
import threading
import time
import traceback

CREATE_NO_WINDOW = 0x08000000 if os.name == "nt" else 0
LOG_PATH = os.path.join(tempfile.gettempdir(), "pathetic_viewport.log")

_ACTIVE_BRIDGES: list[PatheticBridge] = []


def log(message: str) -> None:
    try:
        with open(LOG_PATH, "a", encoding="utf-8") as handle:
            handle.write(f"{time.strftime('%H:%M:%S')} {message}\n")
    except OSError:
        pass


def _read_exact(stream, count: int) -> bytes | None:
    data = bytearray()

    while len(data) < count:
        chunk = stream.read(count - len(data))
        if not chunk:
            return None
        data.extend(chunk)

    return bytes(data)


class PatheticBridge:
    """One persistent Pathetic process fed over stdin; frames arrive on stdout."""

    def __init__(self, executable: str):
        self.executable = executable
        self._process: subprocess.Popen | None = None
        self._write_lock = threading.Lock()
        self._frame_lock = threading.Lock()
        self._latest_frame = None
        self._frame_counter = 0

    @property
    def running(self) -> bool:
        return self._process is not None and self._process.poll() is None

    def start(self) -> None:
        if self.running:
            return

        self.stop()
        log(f"starting bridge: {self.executable}")
        self._process = subprocess.Popen(
            [self.executable, "--bridge"],
            stdin=subprocess.PIPE,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            cwd=os.path.dirname(self.executable) or None,
            creationflags=CREATE_NO_WINDOW,
        )
        threading.Thread(target=self._read_frames, daemon=True).start()
        threading.Thread(target=self._read_stderr, daemon=True).start()
        _ACTIVE_BRIDGES.append(self)

    def _read_stderr(self) -> None:
        stream = self._process.stderr if self._process is not None else None
        if stream is None:
            return

        for line in iter(stream.readline, b""):
            log(f"bridge stderr: {line.decode(errors='replace').rstrip()}")

    def _read_frames(self) -> None:
        stream = self._process.stdout if self._process is not None else None
        if stream is None:
            return

        while True:
            header = _read_exact(stream, 4)
            if header is None:
                log("bridge stdout closed")
                return

            length = struct.unpack("<i", header)[0]
            if length < 12:
                log(f"bridge invalid frame length {length}")
                return

            payload = _read_exact(stream, length)
            if payload is None:
                log("bridge frame payload truncated")
                return

            width, height, frame_index = struct.unpack("<III", payload[:12])
            pixels = payload[12:]
            if len(pixels) != width * height * 4:
                log(f"bridge frame size mismatch: {width}x{height} {len(pixels)} bytes")
                return

            with self._frame_lock:
                self._latest_frame = (width, height, frame_index, pixels)
                self._frame_counter += 1
                counter = self._frame_counter

            if counter == 1 or counter % 30 == 0:
                log(f"frame {frame_index} {width}x{height} ({counter} received)")

    def latest(self):
        with self._frame_lock:
            return self._latest_frame

    def frame_counter(self) -> int:
        with self._frame_lock:
            return self._frame_counter

    def _send(self, message: dict) -> None:
        if not self.running:
            return

        message_type = message.get("type", "?")
        data = (json.dumps(message, separators=(",", ":")) + "\n").encode("utf-8")
        with self._write_lock:
            try:
                self._process.stdin.write(data)
                self._process.stdin.flush()
                log(f"sent {message_type} ({len(data)} bytes)")
            except (BrokenPipeError, OSError):
                log(f"failed to send {message_type}")

    def load(self, request: dict) -> None:
        self._send({"type": "load", "scene": request})

    def camera(self, camera: dict) -> None:
        self._send({"type": "camera", "camera": camera})

    def settings(self, settings) -> None:
        self._send({
            "type": "settings",
            "samplesPerPixel": int(settings.viewport_samples),
            "maxBounces": int(settings.max_bounces),
            "tonemapper": str(settings.tonemapper),
            "exposure": float(settings.exposure),
        })

    def stop(self) -> None:
        process = self._process
        if process is None:
            return

        log("stopping bridge")

        try:
            if process.poll() is None:
                data = (json.dumps({"type": "close"}) + "\n").encode("utf-8")
                process.stdin.write(data)
                process.stdin.flush()
        except (BrokenPipeError, OSError):
            pass

        try:
            process.wait(timeout=1.0)
        except subprocess.TimeoutExpired:
            process.kill()

        self._process = None

        if self in _ACTIVE_BRIDGES:
            _ACTIVE_BRIDGES.remove(self)


def stop_all() -> None:
    for bridge_instance in list(_ACTIVE_BRIDGES):
        bridge_instance.stop()
