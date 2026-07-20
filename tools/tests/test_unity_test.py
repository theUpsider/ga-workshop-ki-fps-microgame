from __future__ import annotations

import argparse
import json
import os
import subprocess
import tempfile
import unittest
from pathlib import Path
from unittest import mock

import sys

TOOLS_DIRECTORY = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(TOOLS_DIRECTORY))

import unity_test


PASSED_XML = """<?xml version="1.0" encoding="utf-8"?>
<test-run testcasecount="2" passed="2" failed="0" skipped="0" inconclusive="0" duration="1.25">
  <test-suite><test-case name="One" fullname="Tests.One" result="Passed" />
  <test-case name="Two" fullname="Tests.Two" result="Passed" /></test-suite>
</test-run>
"""

FAILED_XML = """<?xml version="1.0" encoding="utf-8"?>
<test-run testcasecount="1" passed="0" failed="1" skipped="0" inconclusive="0" duration="0.5">
  <test-suite><test-case name="Fails" fullname="Tests.Fails" result="Failed">
    <failure><message>Expected: True\nBut was: False</message></failure>
  </test-case></test-suite>
</test-run>
"""

EMPTY_XML = """<?xml version="1.0" encoding="utf-8"?>
<test-run testcasecount="0" passed="0" failed="0" skipped="0" inconclusive="0" duration="0" />
"""


class TemporaryProjectTestCase(unittest.TestCase):
    def setUp(self) -> None:
        self.temporary_directory = tempfile.TemporaryDirectory()
        self.root = Path(self.temporary_directory.name)
        (self.root / "Assets").mkdir()
        (self.root / "ProjectSettings").mkdir()
        (self.root / "ProjectSettings" / "ProjectVersion.txt").write_text(
            "m_EditorVersion: 6000.5.0b11\n", encoding="utf-8"
        )
        self.editor = self.root / "6000.5.0b11" / "Editor" / ("Unity.exe" if os.name == "nt" else "Unity")
        self.editor.parent.mkdir(parents=True)
        self.editor.touch()
        self.config = {
            "projectPath": ".",
            "resultsDirectory": "artifacts/unity-tests",
            "unityEditorEnvironmentVariable": "UNITY_EDITOR_TEST",
            "failOnNoTests": True,
            "failOnInconclusive": True,
            "nographics": {"EditMode": True, "PlayMode": False},
            "timeoutsSeconds": {"EditMode": 10, "PlayMode": 10},
            "suites": {
                "fast": [{"platform": "EditMode"}],
                "full": [{"platform": "EditMode"}, {"platform": "PlayMode"}],
            },
        }

    def tearDown(self) -> None:
        self.temporary_directory.cleanup()


class ArgumentTests(unittest.TestCase):
    def test_platform_with_filters_is_accepted(self) -> None:
        args = unity_test.parse_args(
            ["--platform", "EditMode", "--filter", "Tests.One", "--category", "Smoke", "--assembly", "EditTests"]
        )
        self.assertEqual("EditMode", args.platform)
        self.assertEqual(["Tests.One"], args.filters)
        self.assertEqual(["Smoke"], args.category)
        self.assertEqual(["EditTests"], args.assembly)

    def test_missing_selection_is_exit_code_five_error(self) -> None:
        with self.assertRaises(unity_test.CliUsageError):
            unity_test.parse_args([])

    def test_non_positive_timeout_is_rejected(self) -> None:
        with self.assertRaises(unity_test.CliUsageError):
            unity_test.parse_args(["--suite", "fast", "--timeout", "0"])


class EditorResolutionTests(TemporaryProjectTestCase):
    def test_project_version_is_read(self) -> None:
        self.assertEqual("6000.5.0b11", unity_test.read_project_version(self.root))

    def test_environment_editor_with_matching_path_is_used(self) -> None:
        with mock.patch.dict(os.environ, {"UNITY_EDITOR_TEST": str(self.editor)}):
            actual = unity_test.find_unity_editor(self.root, "UNITY_EDITOR_TEST", ci=True)
        self.assertEqual(self.editor.resolve(), actual)

    def test_ci_requires_explicit_editor(self) -> None:
        with mock.patch.dict(os.environ, {}, clear=True):
            with self.assertRaisesRegex(unity_test.InfrastructureError, "must be set in CI"):
                unity_test.find_unity_editor(self.root, "UNITY_EDITOR_TEST", ci=True)


class CommandTests(TemporaryProjectTestCase):
    def test_builds_argument_list_with_all_filters(self) -> None:
        command = unity_test.build_unity_command(
            self.editor,
            self.root,
            "EditMode",
            self.root / "results.xml",
            self.root / "unity.log",
            filters=["Tests.One", "Tests.Two"],
            categories=["Smoke"],
            assemblies=["Game.Tests.EditMode"],
        )
        self.assertIsInstance(command, list)
        self.assertEqual(str(self.editor), command[0])
        self.assertIn("-runTests", command)
        self.assertEqual("Tests.One;Tests.Two", command[command.index("-testFilter") + 1])
        self.assertEqual("Smoke", command[command.index("-testCategory") + 1])
        self.assertEqual("Game.Tests.EditMode", command[command.index("-assemblyNames") + 1])

    def test_nographics_can_be_disabled(self) -> None:
        command = unity_test.build_unity_command(
            self.editor,
            self.root,
            "PlayMode",
            self.root / "results.xml",
            self.root / "unity.log",
            nographics=False,
        )
        self.assertNotIn("-nographics", command)


class ResultTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temporary_directory = tempfile.TemporaryDirectory()
        self.root = Path(self.temporary_directory.name)
        self.xml = self.root / "results.xml"
        self.log = self.root / "unity.log"
        self.log.write_text("Normal Unity output\n", encoding="utf-8")

    def tearDown(self) -> None:
        self.temporary_directory.cleanup()

    def evaluate(self) -> tuple[int, dict]:
        return unity_test.evaluate_result(
            self.xml,
            self.log,
            0,
            timed_out=False,
            fail_on_no_tests=True,
            fail_on_inconclusive=True,
            elapsed_seconds=2.0,
        )

    def test_successful_nunit_xml(self) -> None:
        self.xml.write_text(PASSED_XML, encoding="utf-8")
        code, summary = self.evaluate()
        self.assertEqual(unity_test.EXIT_SUCCESS, code)
        self.assertEqual(2, summary["total"])
        self.assertEqual("passed", summary["status"])

    def test_failed_nunit_xml_includes_test_and_message(self) -> None:
        self.xml.write_text(FAILED_XML, encoding="utf-8")
        code, summary = self.evaluate()
        self.assertEqual(unity_test.EXIT_TEST_FAILURE, code)
        self.assertEqual("Tests.Fails", summary["failedTests"][0]["name"])
        self.assertIn("Expected: True", summary["failedTests"][0]["message"])

    def test_missing_xml_is_infrastructure_error(self) -> None:
        code, summary = self.evaluate()
        self.assertEqual(unity_test.EXIT_INFRASTRUCTURE, code)
        self.assertEqual("infrastructure_error", summary["status"])

    def test_zero_tests_has_dedicated_exit_code(self) -> None:
        self.xml.write_text(EMPTY_XML, encoding="utf-8")
        code, summary = self.evaluate()
        self.assertEqual(unity_test.EXIT_NO_TESTS, code)
        self.assertEqual("no_tests", summary["status"])

    def test_compiler_error_in_log_is_infrastructure_error(self) -> None:
        self.xml.write_text(PASSED_XML, encoding="utf-8")
        self.log.write_text("Assets/File.cs(1,2): error CS1002: ; expected\n", encoding="utf-8")
        code, summary = self.evaluate()
        self.assertEqual(unity_test.EXIT_INFRASTRUCTURE, code)
        self.assertIn("error CS1002", summary["infrastructureErrors"][0])

    def test_timeout_has_dedicated_exit_code(self) -> None:
        code, summary = unity_test.evaluate_result(
            self.xml,
            self.log,
            None,
            timed_out=True,
            fail_on_no_tests=True,
            fail_on_inconclusive=True,
            elapsed_seconds=5.0,
        )
        self.assertEqual(unity_test.EXIT_TIMEOUT, code)
        self.assertEqual("timeout", summary["status"])


class LockTests(TemporaryProjectTestCase):
    def test_existing_live_lock_is_rejected(self) -> None:
        lock_path = self.root / "artifacts" / ".unity-test.lock"
        lock_path.parent.mkdir()
        lock_path.write_text(json.dumps({"pid": os.getpid()}), encoding="utf-8")
        with self.assertRaisesRegex(unity_test.InfrastructureError, "Another Unity test run"):
            unity_test.RunLock(lock_path, self.root).acquire()

    def test_stale_lock_is_replaced_and_released(self) -> None:
        lock_path = self.root / "artifacts" / ".unity-test.lock"
        lock_path.parent.mkdir()
        lock_path.write_text(json.dumps({"pid": 99999999}), encoding="utf-8")
        with unity_test.RunLock(lock_path, self.root):
            self.assertTrue(lock_path.exists())
        self.assertFalse(lock_path.exists())


class FakeSuccessfulProcess:
    pid = 12345

    def __init__(self, command: list[str]) -> None:
        self.command = command

    def wait(self, timeout: int) -> int:
        result = Path(self.command[self.command.index("-testResults") + 1])
        log = Path(self.command[self.command.index("-logFile") + 1])
        result.write_text(PASSED_XML, encoding="utf-8")
        log.write_text("Normal Unity output\n", encoding="utf-8")
        return 0


class FakeTimedOutProcess:
    pid = 12345

    def wait(self, timeout: int) -> int:
        raise subprocess.TimeoutExpired("Unity", timeout)


class FakeConnectedEditorConnection:
    def __init__(self, response: dict, xml: str | None = PASSED_XML, log_text: str = "Connected editor log\n") -> None:
        self.response = response
        self.xml = xml
        self.log_text = log_text
        self.sent: dict | None = None

    def settimeout(self, timeout: float) -> None:
        pass

    def sendall(self, data: bytes) -> None:
        self.sent = json.loads(data.decode("utf-8"))
        if self.xml is not None:
            Path(self.sent["resultsPath"]).write_text(self.xml, encoding="utf-8")
        if "logPath" in self.sent:
            Path(self.sent["logPath"]).write_text(self.log_text, encoding="utf-8")

    def recv(self, size: int) -> bytes:
        return (json.dumps(self.response) + "\n").encode("utf-8")

    def close(self) -> None:
        pass


def _connector_returning(connection: object):
    def connector(address: tuple, timeout: float | None = None) -> object:
        return connection

    return connector


def _unreachable_connector():
    def connector(address: tuple, timeout: float | None = None) -> object:
        raise OSError("connection refused")

    return connector


class ConnectedEditorClientTests(unittest.TestCase):
    def test_returns_none_when_unreachable(self) -> None:
        result = unity_test.run_via_connected_editor(
            "127.0.0.1", 1, 0.01, {"platform": "EditMode"}, 5, connector=_unreachable_connector()
        )
        self.assertIsNone(result)

    def test_returns_parsed_response(self) -> None:
        connection = FakeConnectedEditorConnection({"ok": True}, xml=None)
        result = unity_test.run_via_connected_editor(
            "127.0.0.1", 1, 0.01, {"platform": "EditMode"}, 5, connector=_connector_returning(connection)
        )
        self.assertEqual({"ok": True}, result)


class RunnerTests(TemporaryProjectTestCase):
    def _args(self) -> argparse.Namespace:
        return unity_test.parse_args(["--platform", "EditMode"])

    def test_runner_writes_summary_json(self) -> None:
        def factory(command: list[str], **_: object) -> FakeSuccessfulProcess:
            return FakeSuccessfulProcess(command)

        with mock.patch.dict(os.environ, {"UNITY_EDITOR_TEST": str(self.editor)}):
            runner = unity_test.UnityTestRunner(
                self.config,
                self._args(),
                repository_root=self.root,
                process_factory=factory,
                connected_editor_connector=_unreachable_connector(),
            )
            code = runner.run()
        self.assertEqual(unity_test.EXIT_SUCCESS, code)
        summaries = list((self.root / "artifacts" / "unity-tests").glob("*/summary.json"))
        self.assertEqual(1, len(summaries))
        summary = json.loads(summaries[0].read_text(encoding="utf-8"))
        self.assertEqual("passed", summary["status"])
        self.assertEqual(2, summary["total"])
        self.assertFalse(summary["usedConnectedEditor"])

    def test_runner_uses_connected_editor_when_reachable(self) -> None:
        connection = FakeConnectedEditorConnection({"ok": True})
        factory_calls: list[list[str]] = []

        def factory(command: list[str], **_: object) -> FakeSuccessfulProcess:
            factory_calls.append(command)
            return FakeSuccessfulProcess(command)

        runner = unity_test.UnityTestRunner(
            self.config,
            self._args(),
            repository_root=self.root,
            process_factory=factory,
            connected_editor_connector=_connector_returning(connection),
        )
        code = runner.run()
        self.assertEqual(unity_test.EXIT_SUCCESS, code)
        self.assertEqual([], factory_calls)
        summary_path = next((self.root / "artifacts" / "unity-tests").glob("*/summary.json"))
        summary = json.loads(summary_path.read_text(encoding="utf-8"))
        self.assertTrue(summary["usedConnectedEditor"])
        self.assertEqual("passed", summary["status"])

    def test_runner_falls_back_to_batch_process_when_editor_unreachable(self) -> None:
        def factory(command: list[str], **_: object) -> FakeSuccessfulProcess:
            return FakeSuccessfulProcess(command)

        with mock.patch.dict(os.environ, {"UNITY_EDITOR_TEST": str(self.editor)}):
            runner = unity_test.UnityTestRunner(
                self.config,
                self._args(),
                repository_root=self.root,
                process_factory=factory,
                connected_editor_connector=_unreachable_connector(),
            )
            code = runner.run()
        self.assertEqual(unity_test.EXIT_SUCCESS, code)
        summary_path = next((self.root / "artifacts" / "unity-tests").glob("*/summary.json"))
        summary = json.loads(summary_path.read_text(encoding="utf-8"))
        self.assertFalse(summary["usedConnectedEditor"])

    def test_runner_reports_connected_editor_failure_message(self) -> None:
        connection = FakeConnectedEditorConnection({"ok": False, "error": "boom"}, xml=None)
        runner = unity_test.UnityTestRunner(
            self.config,
            self._args(),
            repository_root=self.root,
            connected_editor_connector=_connector_returning(connection),
        )
        code = runner.run()
        self.assertEqual(unity_test.EXIT_INFRASTRUCTURE, code)
        summary_path = next((self.root / "artifacts" / "unity-tests").glob("*/summary.json"))
        summary = json.loads(summary_path.read_text(encoding="utf-8"))
        self.assertIn("boom", summary["infrastructureErrors"][0])

    def test_runner_reports_connected_editor_timeout(self) -> None:
        connection = FakeConnectedEditorConnection({"ok": False, "timedOut": True}, xml=None)
        runner = unity_test.UnityTestRunner(
            self.config,
            self._args(),
            repository_root=self.root,
            connected_editor_connector=_connector_returning(connection),
        )
        code = runner.run()
        self.assertEqual(unity_test.EXIT_TIMEOUT, code)

    def test_runner_terminates_process_and_writes_timeout_summary(self) -> None:
        terminated: list[int] = []

        def factory(command: list[str], **_: object) -> FakeTimedOutProcess:
            return FakeTimedOutProcess()

        def terminate(process: FakeTimedOutProcess) -> None:
            terminated.append(process.pid)

        with mock.patch.dict(os.environ, {"UNITY_EDITOR_TEST": str(self.editor)}):
            runner = unity_test.UnityTestRunner(
                self.config,
                self._args(),
                repository_root=self.root,
                process_factory=factory,
                process_terminator=terminate,
                connected_editor_connector=_unreachable_connector(),
            )
            code = runner.run()
        self.assertEqual(unity_test.EXIT_TIMEOUT, code)
        self.assertEqual([12345], terminated)
        summary_path = next((self.root / "artifacts" / "unity-tests").glob("*/summary.json"))
        summary = json.loads(summary_path.read_text(encoding="utf-8"))
        self.assertEqual("timeout", summary["status"])
        self.assertEqual(unity_test.EXIT_TIMEOUT, summary["exitCode"])


if __name__ == "__main__":
    unittest.main()
