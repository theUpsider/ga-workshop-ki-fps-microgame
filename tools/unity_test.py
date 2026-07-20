#!/usr/bin/env python3
"""Repository-owned entrypoint for deterministic Unity Test Framework runs."""

from __future__ import annotations

import argparse
import datetime as dt
import json
import os
import re
import shlex
import signal
import socket
import subprocess
import sys
import time
import xml.etree.ElementTree as ET
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Callable, Iterable, Sequence


EXIT_SUCCESS = 0
EXIT_TEST_FAILURE = 1
EXIT_INFRASTRUCTURE = 2
EXIT_TIMEOUT = 3
EXIT_NO_TESTS = 4
EXIT_INVALID_ARGUMENTS = 5

PLATFORMS = ("EditMode", "PlayMode")
DEFAULT_CONNECTED_EDITOR_HOST = "127.0.0.1"
DEFAULT_CONNECTED_EDITOR_PORT = 17930
DEFAULT_CONNECTED_EDITOR_CONNECT_TIMEOUT_SECONDS = 1.0
SCRIPT_PATH = Path(__file__).resolve()
REPOSITORY_ROOT = SCRIPT_PATH.parent.parent
DEFAULT_CONFIG_PATH = SCRIPT_PATH.with_name("unity_test_config.json")

LOG_ERROR_PATTERNS = (
    re.compile(r"\berror CS\d{4}\b", re.IGNORECASE),
    re.compile(r"scripts have compiler errors", re.IGNORECASE),
    re.compile(r"compilation (?:failed|had errors)", re.IGNORECASE),
    re.compile(r"failed to (?:resolve|update) packages", re.IGNORECASE),
    re.compile(r"no valid unity editor license", re.IGNORECASE),
    re.compile(r"license.*(?:failed|error|invalid)", re.IGNORECASE),
    re.compile(r"aborting batchmode due to failure", re.IGNORECASE),
)


class CliUsageError(Exception):
    """Raised for command-line errors that must map to exit code 5."""


class InfrastructureError(Exception):
    """Raised for configuration or Unity infrastructure failures."""


class UnityArgumentParser(argparse.ArgumentParser):
    def error(self, message: str) -> None:
        raise CliUsageError(message)


@dataclass
class NUnitResult:
    total: int = 0
    passed: int = 0
    failed: int = 0
    skipped: int = 0
    inconclusive: int = 0
    duration_seconds: float = 0.0
    failed_tests: list[dict[str, str]] = field(default_factory=list)


def positive_int(value: str) -> int:
    try:
        parsed = int(value)
    except ValueError as exc:
        raise argparse.ArgumentTypeError("must be an integer") from exc
    if parsed <= 0:
        raise argparse.ArgumentTypeError("must be greater than zero")
    return parsed


def non_empty(value: str) -> str:
    stripped = value.strip()
    if not stripped:
        raise argparse.ArgumentTypeError("must not be empty")
    return stripped


def create_parser() -> UnityArgumentParser:
    parser = UnityArgumentParser(
        description="Run this repository's Unity tests through the supported interface."
    )
    selection = parser.add_mutually_exclusive_group(required=True)
    selection.add_argument("--suite", help="Configured suite name, for example fast or full")
    selection.add_argument("--platform", choices=PLATFORMS, help="Run one test platform")
    parser.add_argument(
        "--filter",
        dest="filters",
        action="append",
        default=[],
        type=non_empty,
        help="Fully qualified test filter; repeat to specify more than one",
    )
    parser.add_argument(
        "--category",
        action="append",
        default=[],
        type=non_empty,
        help="NUnit category; repeat to specify more than one",
    )
    parser.add_argument(
        "--assembly",
        action="append",
        default=[],
        type=non_empty,
        help="Test assembly; repeat to specify more than one",
    )
    parser.add_argument("--ci", action="store_true", help="Require UNITY_EDITOR and use CI output")
    parser.add_argument("--timeout", type=positive_int, help="Override timeout in seconds")
    parser.add_argument(
        "--keep-going",
        action="store_true",
        help="Continue later platforms in a suite after a failure",
    )
    return parser


def parse_args(argv: Sequence[str] | None = None) -> argparse.Namespace:
    return create_parser().parse_args(argv)


def load_config(path: Path = DEFAULT_CONFIG_PATH) -> dict[str, Any]:
    try:
        config = json.loads(path.read_text(encoding="utf-8"))
    except FileNotFoundError as exc:
        raise InfrastructureError(f"Configuration file not found: {path}") from exc
    except (OSError, json.JSONDecodeError) as exc:
        raise InfrastructureError(f"Cannot read configuration file {path}: {exc}") from exc

    required = {
        "projectPath",
        "resultsDirectory",
        "unityEditorEnvironmentVariable",
        "failOnNoTests",
        "failOnInconclusive",
        "timeoutsSeconds",
        "suites",
    }
    missing = sorted(required.difference(config))
    if missing:
        raise InfrastructureError(f"Configuration is missing: {', '.join(missing)}")
    return config


def resolve_project_path(config: dict[str, Any], repository_root: Path = REPOSITORY_ROOT) -> Path:
    project_path = Path(str(config["projectPath"]))
    if not project_path.is_absolute():
        project_path = repository_root / project_path
    project_path = project_path.resolve()
    if not (project_path / "Assets").is_dir() or not (project_path / "ProjectSettings").is_dir():
        raise InfrastructureError(f"Configured projectPath is not a Unity project: {project_path}")
    return project_path


def read_project_version(project_path: Path) -> str:
    version_file = project_path / "ProjectSettings" / "ProjectVersion.txt"
    try:
        contents = version_file.read_text(encoding="utf-8")
    except OSError as exc:
        raise InfrastructureError(f"Cannot read Unity project version: {version_file}") from exc
    match = re.search(r"^m_EditorVersion:\s*(\S+)\s*$", contents, re.MULTILINE)
    if not match:
        raise InfrastructureError(f"m_EditorVersion is missing from {version_file}")
    return match.group(1)


def _expanded_editor_path(value: str) -> Path:
    return Path(os.path.expandvars(os.path.expanduser(value))).resolve()


def _path_contains_version(editor_path: Path, expected_version: str) -> bool:
    return expected_version in editor_path.parts


def _query_editor_version(editor_path: Path) -> str:
    try:
        completed = subprocess.run(
            [str(editor_path), "-version", "-batchmode", "-quit"],
            capture_output=True,
            text=True,
            timeout=30,
            check=False,
        )
    except (OSError, subprocess.TimeoutExpired) as exc:
        raise InfrastructureError(f"Cannot verify Unity editor version at {editor_path}: {exc}") from exc
    output = f"{completed.stdout}\n{completed.stderr}"
    match = re.search(r"\b\d+\.\d+\.\d+[abfp]\d+\b", output)
    if completed.returncode != 0 or not match:
        raise InfrastructureError(f"Unity editor version could not be verified: {editor_path}")
    return match.group(0)


def _validate_editor_version(editor_path: Path, expected_version: str) -> None:
    actual_version = (
        expected_version
        if _path_contains_version(editor_path, expected_version)
        else _query_editor_version(editor_path)
    )
    if actual_version != expected_version:
        raise InfrastructureError(
            f"Unity version mismatch: project requires {expected_version}, editor is {actual_version}"
        )


def known_unity_editor_paths(expected_version: str) -> list[Path]:
    candidates: list[Path] = []
    if sys.platform == "win32":
        for variable in ("ProgramFiles", "ProgramFiles(x86)"):
            base = os.environ.get(variable)
            if base:
                candidates.append(
                    Path(base) / "Unity" / "Hub" / "Editor" / expected_version / "Editor" / "Unity.exe"
                )
    elif sys.platform == "darwin":
        candidates.append(
            Path("/Applications/Unity/Hub/Editor")
            / expected_version
            / "Unity.app/Contents/MacOS/Unity"
        )
    else:
        candidates.extend(
            [
                Path.home() / "Unity" / "Hub" / "Editor" / expected_version / "Editor" / "Unity",
                Path("/opt/unity/editors") / expected_version / "Editor" / "Unity",
            ]
        )
    return candidates


def find_unity_editor(
    project_path: Path,
    environment_variable: str,
    ci: bool,
) -> Path:
    expected_version = read_project_version(project_path)
    configured = os.environ.get(environment_variable)
    if configured:
        editor = _expanded_editor_path(configured)
        if not editor.is_file():
            raise InfrastructureError(f"{environment_variable} does not point to a file: {editor}")
        _validate_editor_version(editor, expected_version)
        return editor
    if ci:
        raise InfrastructureError(f"{environment_variable} must be set in CI")
    for candidate in known_unity_editor_paths(expected_version):
        if candidate.is_file():
            return candidate.resolve()
    searched = ", ".join(str(path) for path in known_unity_editor_paths(expected_version))
    raise InfrastructureError(
        f"Unity {expected_version} was not found. Set {environment_variable}. Searched: {searched}"
    )


def _combined_values(target: dict[str, Any], key: str, cli_values: Sequence[str]) -> list[str]:
    configured = target.get(key, [])
    if isinstance(configured, str):
        configured = [configured]
    if not isinstance(configured, list) or not all(isinstance(item, str) for item in configured):
        raise InfrastructureError(f"Suite field '{key}' must be a string list")
    return [*configured, *cli_values]


def build_unity_command(
    editor_path: Path,
    project_path: Path,
    platform: str,
    result_path: Path,
    log_path: Path,
    *,
    filters: Sequence[str] = (),
    categories: Sequence[str] = (),
    assemblies: Sequence[str] = (),
    nographics: bool = True,
) -> list[str]:
    if platform not in PLATFORMS:
        raise InfrastructureError(f"Unsupported Unity test platform: {platform}")
    command = [str(editor_path), "-batchmode"]
    if nographics:
        command.append("-nographics")
    command.extend(
        [
            "-quit",
            "-projectPath",
            str(project_path),
            "-runTests",
            "-testPlatform",
            platform,
            "-testResults",
            str(result_path),
            "-logFile",
            str(log_path),
        ]
    )
    if filters:
        command.extend(["-testFilter", ";".join(filters)])
    if categories:
        command.extend(["-testCategory", ";".join(categories)])
    if assemblies:
        command.extend(["-assemblyNames", ";".join(assemblies)])
    return command


def connected_editor_settings(config: dict[str, Any]) -> dict[str, Any]:
    """Read the optional 'connectedEditor' section, applying defaults for missing fields."""
    configured = config.get("connectedEditor", {})
    if not isinstance(configured, dict):
        configured = {}
    return {
        "enabled": bool(configured.get("enabled", True)),
        "host": str(configured.get("host", DEFAULT_CONNECTED_EDITOR_HOST)),
        "port": int(configured.get("port", DEFAULT_CONNECTED_EDITOR_PORT)),
        "connectTimeoutSeconds": float(
            configured.get("connectTimeoutSeconds", DEFAULT_CONNECTED_EDITOR_CONNECT_TIMEOUT_SECONDS)
        ),
    }


def run_via_connected_editor(
    host: str,
    port: int,
    connect_timeout: float,
    payload: dict[str, Any],
    run_timeout: float,
    connector: Callable[..., Any] = socket.create_connection,
) -> dict[str, Any] | None:
    """Ask an already-running Unity Editor (with the TestRunnerApi socket server loaded) to
    execute a run. Returns None when no such editor is reachable, so the caller can fall back
    to launching a headless Unity process instead."""
    try:
        connection = connector((host, port), timeout=connect_timeout)
    except OSError:
        return None
    try:
        connection.settimeout(run_timeout)
        connection.sendall((json.dumps(payload) + "\n").encode("utf-8"))
        buffer = b""
        while not buffer.endswith(b"\n"):
            chunk = connection.recv(65536)
            if not chunk:
                break
            buffer += chunk
    except socket.timeout:
        return {"ok": False, "timedOut": True, "error": "Connected editor run exceeded its timeout"}
    except OSError as exc:
        return {"ok": False, "error": f"Connected editor communication failed: {exc}"}
    finally:
        try:
            connection.close()
        except OSError:
            pass
    if not buffer.strip():
        return {
            "ok": False,
            "error": (
                "Connected editor closed the connection without a response. For PlayMode this "
                "usually means Unity performed a domain reload while entering play mode, which "
                "drops the socket. Either run this platform through the batch-mode fallback "
                "(close the Editor GUI first, or use a separate worktree/CI runner), or set "
                "Project Settings > Editor > Enter Play Mode Options to disable domain reload."
            ),
        }
    try:
        return json.loads(buffer.decode("utf-8"))
    except json.JSONDecodeError as exc:
        return {"ok": False, "error": f"Connected editor sent an invalid response: {exc}"}


def _safe_float(value: str | None) -> float:
    try:
        return float(value or 0)
    except ValueError:
        return 0.0


def _message_for_test(test_case: ET.Element) -> str:
    message = test_case.findtext("./failure/message") or test_case.findtext("./reason/message") or ""
    return " ".join(message.strip().split())


def parse_nunit_xml(result_path: Path) -> NUnitResult:
    try:
        root = ET.parse(result_path).getroot()
    except (OSError, ET.ParseError) as exc:
        raise InfrastructureError(f"Cannot parse NUnit XML {result_path}: {exc}") from exc

    cases = list(root.iter("test-case"))
    result = NUnitResult(duration_seconds=_safe_float(root.get("duration") or root.get("time")))
    if cases:
        result.total = len(cases)
        for case in cases:
            outcome = (case.get("result") or "").strip().lower()
            if outcome in {"passed", "success"}:
                result.passed += 1
            elif outcome in {"failed", "failure", "error"}:
                result.failed += 1
                result.failed_tests.append(
                    {"name": case.get("fullname") or case.get("name") or "<unnamed>", "message": _message_for_test(case)}
                )
            elif outcome in {"inconclusive"}:
                result.inconclusive += 1
            else:
                result.skipped += 1
        return result

    result.total = int(root.get("testcasecount") or root.get("total") or 0)
    result.passed = int(root.get("passed") or root.get("successes") or 0)
    result.failed = int(root.get("failed") or root.get("failures") or root.get("errors") or 0)
    result.inconclusive = int(root.get("inconclusive") or 0)
    result.skipped = int(root.get("skipped") or root.get("not-run") or 0)
    return result


def detect_log_errors(log_path: Path, limit: int = 10) -> list[str]:
    try:
        lines = log_path.read_text(encoding="utf-8", errors="replace").splitlines()
    except OSError:
        return []
    matches: list[str] = []
    for line in lines:
        compact = " ".join(line.strip().split())
        if compact and any(pattern.search(compact) for pattern in LOG_ERROR_PATTERNS):
            if compact not in matches:
                matches.append(compact)
            if len(matches) >= limit:
                break
    return matches


def evaluate_result(
    result_path: Path,
    log_path: Path,
    process_return_code: int | None,
    *,
    timed_out: bool,
    fail_on_no_tests: bool,
    fail_on_inconclusive: bool,
    elapsed_seconds: float,
    explicit_infrastructure_error: str | None = None,
) -> tuple[int, dict[str, Any]]:
    base: dict[str, Any] = {
        "status": "infrastructure_error",
        "total": 0,
        "passed": 0,
        "failed": 0,
        "skipped": 0,
        "inconclusive": 0,
        "durationSeconds": round(elapsed_seconds, 3),
        "failedTests": [],
        "infrastructureErrors": [],
    }
    if timed_out:
        base["status"] = "timeout"
        base["infrastructureErrors"] = ["Unity test execution exceeded its timeout"]
        return EXIT_TIMEOUT, base

    log_errors = detect_log_errors(log_path)
    if explicit_infrastructure_error:
        log_errors = [explicit_infrastructure_error, *log_errors]
    if not result_path.is_file():
        base["infrastructureErrors"] = log_errors or ["Unity did not create the NUnit XML result"]
        return EXIT_INFRASTRUCTURE, base

    try:
        result = parse_nunit_xml(result_path)
    except InfrastructureError as exc:
        base["infrastructureErrors"] = [str(exc), *log_errors]
        return EXIT_INFRASTRUCTURE, base

    base.update(
        {
            "total": result.total,
            "passed": result.passed,
            "failed": result.failed,
            "skipped": result.skipped,
            "inconclusive": result.inconclusive,
            "durationSeconds": round(result.duration_seconds or elapsed_seconds, 3),
            "failedTests": result.failed_tests,
        }
    )
    if log_errors:
        base["infrastructureErrors"] = log_errors
        return EXIT_INFRASTRUCTURE, base
    if fail_on_no_tests and result.total == 0:
        base["status"] = "no_tests"
        return EXIT_NO_TESTS, base
    if result.failed > 0 or (fail_on_inconclusive and result.inconclusive > 0):
        base["status"] = "failed"
        return EXIT_TEST_FAILURE, base
    if process_return_code not in (0, None):
        base["infrastructureErrors"] = [f"Unity exited with code {process_return_code}"]
        return EXIT_INFRASTRUCTURE, base
    base["status"] = "passed"
    return EXIT_SUCCESS, base


def _pid_is_alive(pid: int) -> bool:
    if pid <= 0:
        return False
    try:
        os.kill(pid, 0)
    except ProcessLookupError:
        return False
    except PermissionError:
        return True
    except OSError:
        return False
    return True


class RunLock:
    def __init__(self, path: Path, project_path: Path) -> None:
        self.path = path
        self.project_path = project_path
        self.pid = os.getpid()
        self.acquired = False

    def acquire(self) -> None:
        self.path.parent.mkdir(parents=True, exist_ok=True)
        payload = {
            "pid": self.pid,
            "projectPath": str(self.project_path),
            "createdAt": dt.datetime.now(dt.timezone.utc).isoformat(),
        }
        for _ in range(2):
            try:
                descriptor = os.open(self.path, os.O_CREAT | os.O_EXCL | os.O_WRONLY)
            except FileExistsError:
                try:
                    existing = json.loads(self.path.read_text(encoding="utf-8"))
                    existing_pid = int(existing["pid"])
                except (OSError, ValueError, KeyError, json.JSONDecodeError) as exc:
                    raise InfrastructureError(
                        f"Unity test lock exists and cannot be proven stale: {self.path}"
                    ) from exc
                if _pid_is_alive(existing_pid):
                    raise InfrastructureError(
                        f"Another Unity test run holds {self.path} (PID {existing_pid})"
                    )
                try:
                    self.path.unlink()
                except OSError as exc:
                    raise InfrastructureError(f"Cannot remove stale Unity test lock: {self.path}") from exc
                continue
            try:
                with os.fdopen(descriptor, "w", encoding="utf-8") as handle:
                    json.dump(payload, handle, indent=2)
                    handle.write("\n")
            except Exception:
                self.path.unlink(missing_ok=True)
                raise
            self.acquired = True
            return
        raise InfrastructureError(f"Could not acquire Unity test lock: {self.path}")

    def release(self) -> None:
        if not self.acquired:
            return
        try:
            existing = json.loads(self.path.read_text(encoding="utf-8"))
            if int(existing.get("pid", -1)) == self.pid:
                self.path.unlink(missing_ok=True)
        except (OSError, ValueError, json.JSONDecodeError):
            pass
        self.acquired = False

    def __enter__(self) -> "RunLock":
        self.acquire()
        return self

    def __exit__(self, *_: object) -> None:
        self.release()


def terminate_process_tree(process: subprocess.Popen[Any]) -> None:
    if process.poll() is not None:
        return
    if os.name == "nt":
        subprocess.run(
            ["taskkill", "/PID", str(process.pid), "/T", "/F"],
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
            check=False,
        )
    else:
        try:
            os.killpg(os.getpgid(process.pid), signal.SIGKILL)
        except (ProcessLookupError, PermissionError):
            pass
    if process.poll() is None:
        process.kill()


def _command_text(command: Sequence[str]) -> str:
    return subprocess.list2cmdline(command) if os.name == "nt" else shlex.join(command)


def _run_id(platform: str) -> str:
    timestamp = dt.datetime.now(dt.timezone.utc).strftime("%Y-%m-%dT%H%M%S.%fZ")
    return f"{timestamp}-{platform.lower()}"


def _write_json(path: Path, value: dict[str, Any]) -> None:
    path.write_text(json.dumps(value, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


def _nographics_for(config: dict[str, Any], platform: str, target: dict[str, Any]) -> bool:
    if "nographics" in target:
        return bool(target["nographics"])
    configured = config.get("nographics", True)
    if isinstance(configured, dict):
        return bool(configured.get(platform, True))
    return bool(configured)


def _targets_for(args: argparse.Namespace, config: dict[str, Any]) -> list[dict[str, Any]]:
    if args.platform:
        return [{"platform": args.platform}]
    suites = config.get("suites", {})
    if args.suite not in suites:
        available = ", ".join(sorted(suites)) or "<none>"
        raise CliUsageError(f"unknown suite '{args.suite}'; available suites: {available}")
    targets = suites[args.suite]
    if not isinstance(targets, list) or not targets:
        raise InfrastructureError(f"Suite '{args.suite}' must contain at least one target")
    normalized: list[dict[str, Any]] = []
    for target in targets:
        if not isinstance(target, dict) or target.get("platform") not in PLATFORMS:
            raise InfrastructureError(f"Suite '{args.suite}' contains an invalid platform target")
        normalized.append(target)
    return normalized


class UnityTestRunner:
    def __init__(
        self,
        config: dict[str, Any],
        args: argparse.Namespace,
        *,
        repository_root: Path = REPOSITORY_ROOT,
        process_factory: Callable[..., subprocess.Popen[Any]] = subprocess.Popen,
        process_terminator: Callable[[subprocess.Popen[Any]], None] = terminate_process_tree,
        connected_editor_connector: Callable[..., Any] = socket.create_connection,
    ) -> None:
        self.config = config
        self.args = args
        self.repository_root = repository_root
        self.project_path = resolve_project_path(config, repository_root)
        results = Path(str(config["resultsDirectory"]))
        self.results_directory = (repository_root / results).resolve() if not results.is_absolute() else results.resolve()
        self.process_factory = process_factory
        self.process_terminator = process_terminator
        self.connected_editor_connector = connected_editor_connector
        self._editor_path_cache: Path | None = None

    def run(self) -> int:
        targets = _targets_for(self.args, self.config)
        lock = RunLock(self.results_directory / ".unity-test.lock", self.project_path)
        try:
            with lock:
                return self._run_targets(targets)
        except InfrastructureError as exc:
            self._write_preflight_failure(targets[0]["platform"], str(exc))
            return EXIT_INFRASTRUCTURE

    def _editor_path(self) -> Path:
        # Resolved lazily: a connected, already-running Editor never needs this.
        if self._editor_path_cache is None:
            self._editor_path_cache = find_unity_editor(
                self.project_path,
                str(self.config["unityEditorEnvironmentVariable"]),
                self.args.ci,
            )
        return self._editor_path_cache

    def _run_targets(self, targets: list[dict[str, Any]]) -> int:
        exit_codes: list[int] = []
        for target in targets:
            code = self._run_platform(target)
            exit_codes.append(code)
            if code != EXIT_SUCCESS and not self.args.keep_going:
                break
        for code in (EXIT_TIMEOUT, EXIT_INFRASTRUCTURE, EXIT_NO_TESTS, EXIT_TEST_FAILURE):
            if code in exit_codes:
                return code
        return EXIT_SUCCESS

    def _run_platform(self, target: dict[str, Any]) -> int:
        platform = target["platform"]
        run_directory = self.results_directory / _run_id(platform)
        run_directory.mkdir(parents=True, exist_ok=False)
        temp_directory = run_directory / "temp"
        temp_directory.mkdir()
        result_path = run_directory / "results.xml"
        log_path = run_directory / "unity.log"
        summary_path = run_directory / "summary.json"
        command_path = run_directory / "command.txt"
        filters = _combined_values(target, "filters", self.args.filters)
        categories = _combined_values(target, "categories", self.args.category)
        assemblies = _combined_values(target, "assemblies", self.args.assembly)
        timeout = self.args.timeout or int(self.config["timeoutsSeconds"][platform])

        used_connected_editor = False
        timed_out = False
        return_code: int | None = None
        connected_editor_error: str | None = None
        started = time.monotonic()

        connected = connected_editor_settings(self.config)
        if connected["enabled"]:
            response = run_via_connected_editor(
                connected["host"],
                connected["port"],
                connected["connectTimeoutSeconds"],
                {
                    "platform": platform,
                    "filters": filters,
                    "categories": categories,
                    "assemblies": assemblies,
                    "resultsPath": str(result_path),
                    "logPath": str(log_path),
                },
                timeout,
                connector=self.connected_editor_connector,
            )
            if response is not None:
                used_connected_editor = True
                started = time.monotonic()
                command_path.write_text(
                    f"CONNECTED_EDITOR {connected['host']}:{connected['port']} platform={platform}\n",
                    encoding="utf-8",
                )
                if response.get("timedOut"):
                    timed_out = True
                elif not response.get("ok", False):
                    return_code = 1
                    connected_editor_error = str(response.get("error") or "Connected editor run failed")
                    if not log_path.exists() or not log_path.read_text(encoding="utf-8", errors="replace").strip():
                        log_path.write_text(connected_editor_error + "\n", encoding="utf-8")
                else:
                    return_code = 0

        if not used_connected_editor:
            editor = self._editor_path()
            command = build_unity_command(
                editor,
                self.project_path,
                platform,
                result_path,
                log_path,
                filters=filters,
                categories=categories,
                assemblies=assemblies,
                nographics=_nographics_for(self.config, platform, target),
            )
            command_path.write_text(_command_text(command) + "\n", encoding="utf-8")
            environment = os.environ.copy()
            environment.update({"TMP": str(temp_directory), "TEMP": str(temp_directory), "TMPDIR": str(temp_directory)})
            popen_kwargs: dict[str, Any] = {
                "cwd": str(self.project_path),
                "env": environment,
                "stdout": subprocess.DEVNULL,
                "stderr": subprocess.STDOUT,
            }
            if os.name == "nt":
                popen_kwargs["creationflags"] = getattr(subprocess, "CREATE_NEW_PROCESS_GROUP", 0)
            else:
                popen_kwargs["start_new_session"] = True

            started = time.monotonic()
            try:
                process = self.process_factory(command, **popen_kwargs)
                try:
                    return_code = process.wait(timeout=timeout)
                except subprocess.TimeoutExpired:
                    timed_out = True
                    self.process_terminator(process)
                    try:
                        process.wait(timeout=10)
                    except subprocess.TimeoutExpired:
                        pass
            except OSError as exc:
                log_path.write_text(f"Failed to start Unity: {exc}\n", encoding="utf-8")
        elapsed = time.monotonic() - started
        log_path.touch(exist_ok=True)
        exit_code, summary = evaluate_result(
            result_path,
            log_path,
            return_code,
            timed_out=timed_out,
            fail_on_no_tests=bool(self.config["failOnNoTests"]),
            fail_on_inconclusive=bool(self.config["failOnInconclusive"]),
            elapsed_seconds=elapsed,
            explicit_infrastructure_error=connected_editor_error,
        )
        summary.update(
            {
                "platform": platform,
                "exitCode": exit_code,
                "resultFile": "results.xml",
                "logFile": "unity.log",
                "commandFile": "command.txt",
                "usedConnectedEditor": used_connected_editor,
            }
        )
        _write_json(summary_path, summary)
        self._print_summary(summary, result_path, log_path, summary_path)
        return exit_code

    def _write_preflight_failure(self, platform: str, message: str) -> None:
        run_directory = self.results_directory / _run_id(platform)
        run_directory.mkdir(parents=True, exist_ok=False)
        log_path = run_directory / "unity.log"
        summary_path = run_directory / "summary.json"
        log_path.write_text(message + "\n", encoding="utf-8")
        summary = {
            "status": "infrastructure_error",
            "platform": platform,
            "total": 0,
            "passed": 0,
            "failed": 0,
            "skipped": 0,
            "inconclusive": 0,
            "durationSeconds": 0.0,
            "failedTests": [],
            "infrastructureErrors": [message],
            "exitCode": EXIT_INFRASTRUCTURE,
            "resultFile": None,
            "logFile": "unity.log",
            "commandFile": None,
        }
        _write_json(summary_path, summary)
        self._print_summary(summary, None, log_path, summary_path)

    @staticmethod
    def _print_summary(
        summary: dict[str, Any],
        result_path: Path | None,
        log_path: Path,
        summary_path: Path,
    ) -> None:
        passed = summary["status"] == "passed"
        print("UNITY TESTS PASSED" if passed else "UNITY TESTS FAILED")
        print(f"Platform: {summary['platform']}")
        print(f"Total: {summary['total']}")
        print(f"Passed: {summary['passed']}")
        print(f"Failed: {summary['failed']}")
        print(f"Duration: {summary['durationSeconds']:.3f} s")
        for index, failure in enumerate(summary.get("failedTests", []), start=1):
            print(f"\n{index}. {failure['name']}")
            print(f"   {failure['message'] or 'No failure message was provided.'}")
        for error in summary.get("infrastructureErrors", []):
            print(f"Infrastructure: {error}")
        if result_path is not None:
            print(f"Results: {result_path}")
        print(f"Summary: {summary_path}")
        print(f"Full log: {log_path}")


def main(argv: Sequence[str] | None = None) -> int:
    try:
        args = parse_args(argv)
        config = load_config()
        return UnityTestRunner(config, args).run()
    except CliUsageError as exc:
        print(f"Invalid arguments: {exc}", file=sys.stderr)
        return EXIT_INVALID_ARGUMENTS
    except InfrastructureError as exc:
        print(f"Infrastructure error: {exc}", file=sys.stderr)
        return EXIT_INFRASTRUCTURE


if __name__ == "__main__":
    raise SystemExit(main())
