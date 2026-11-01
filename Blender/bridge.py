from __future__ import annotations

import json
import os
import struct
import subprocess
import threading

CREATE_NO_WINDOW = 0x08000000 if os.name == "nt" else 0

_ACTIVE_BRIDGES: list[PatheticBridge] = []


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
        self._process = subprocess.Popen(
            [self.executable, "--bridge"],
            stdin=subprocess.PIPE,
            stdout=subprocess.PIPE,
            stderr=subprocess.DEVNULL,
            cwd=os.path.dirname(self.executable) or None,
            creationflags=CREATE_NO_WINDOW,
        )
        threading.Thread(target=self._read_frames, daemon=True).start()
        _ACTIVE_BRIDGES.append(self)

    def _read_frames(self) -> None:
        stream = self._process.stdout if self._process is not None else None
        if stream is None:
            return

        while True:
            header = _read_exact(stream, 4)
            if header is None:
                return

            length = struct.unpack("<i", header)[0]
            if length < 12:
                return

            payload = _read_exact(stream, length)
            if payload is None:
                return

            width, height, frame_index = struct.unpack("<III", payload[:12])
            pixels = payload[12:]
            if len(pixels) != width * height * 4:
                return

            with self._frame_lock:
                self._latest_frame = (width, height, frame_index, pixels)
                self._frame_counter += 1

    def latest(self):
        with self._frame_lock:
            return self._latest_frame

    def frame_counter(self) -> int:
        with self._frame_lock:
            return self._frame_counter

    def _send(self, message: dict) -> None:
        if not self.running:
            return

        data = (json.dumps(message, separators=(",", ":")) + "\n").encode("utf-8")
        with self._write_lock:
            try:
                self._process.stdin.write(data)
                self._process.stdin.flush()
            except (BrokenPipeError, OSError):
                pass

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
