"""PatchPony read-only Open WebUI Pipe.

Import this source as an administrator-managed Pipe Function in Open WebUI.
Only a minimal, controlled request reaches n8n; n8n owns subsequent agent
orchestration and PatchPony calls.
"""

import asyncio
import hashlib
import re
from typing import Any
from uuid import uuid4

import httpx
from pydantic import BaseModel, Field


class Pipe:
    class Valves(BaseModel):
        N8N_WEBHOOK_URL: str = Field(
            default="",
            description="Trusted HTTPS n8n webhook URL for the PatchPony read-only workflow.",
        )
        N8N_WEBHOOK_TOKEN: str = Field(
            default="",
            description="Shared secret sent only to the trusted n8n webhook.",
        )
        DEFAULT_PROJECT_ID: str = Field(
            default="",
            description="Administrator-controlled project ID for this read-only pilot pipe.",
        )
        REQUEST_TIMEOUT_SECONDS: int = Field(
            default=60,
            ge=1,
            le=120,
            description="Maximum time to wait for the synchronous n8n webhook response.",
        )

    def __init__(self) -> None:
        self.valves = self.Valves()

    def pipes(self) -> list[dict[str, str]]:
        return [{"id": "patchpony-readonly", "name": "PatchPony · Read-only"}]

    async def pipe(
        self,
        body: dict[str, Any],
        __user__: dict[str, Any] | None = None,
    ) -> str:
        if not self._is_configured():
            return "PatchPony ist noch nicht vollständig konfiguriert. Bitte Pipe-Einstellungen durch einen Administrator prüfen lassen."

        question = self._latest_user_question(body)
        requester = self._pseudonymous_requester(__user__)
        if not question:
            return "Bitte stelle eine Frage zu Code oder freigegebenem Wissen."
        if requester is None:
            return "PatchPony konnte deine Chat-Identität nicht sicher zuordnen. Bitte melde dich erneut an."

        payload = {
            "requestId": str(uuid4()),
            "source": "open-webui",
            "projectId": self.valves.DEFAULT_PROJECT_ID,
            "requester": requester,
            "question": question,
        }

        try:
            async with httpx.AsyncClient(timeout=self.valves.REQUEST_TIMEOUT_SECONDS) as client:
                response = await client.post(
                    self.valves.N8N_WEBHOOK_URL,
                    json=payload,
                    headers={"X-PatchPony-Webhook-Token": self.valves.N8N_WEBHOOK_TOKEN},
                )
                response.raise_for_status()
        except asyncio.CancelledError:
            raise
        except httpx.TimeoutException:
            return "PatchPony hat nicht rechtzeitig geantwortet. Bitte versuche es erneut."
        except httpx.HTTPStatusError as error:
            return self._upstream_error(error.response.status_code)
        except httpx.RequestError:
            return "PatchPony ist derzeit nicht erreichbar. Bitte versuche es später erneut."

        return self._answer(response)

    @staticmethod
    def _upstream_error(status_code: int) -> str:
        if status_code in {408, 504}:
            return "PatchPony hat nicht rechtzeitig geantwortet. Bitte versuche es erneut."
        if status_code == 429:
            return "PatchPony ist gerade ausgelastet. Bitte warte kurz und versuche es erneut."
        return "PatchPony ist derzeit nicht erreichbar. Bitte versuche es später erneut."

    def _is_configured(self) -> bool:
        return (
            bool(self.valves.N8N_WEBHOOK_URL.strip())
            and bool(self.valves.N8N_WEBHOOK_TOKEN.strip())
            and bool(re.fullmatch(r"[a-z0-9][a-z0-9-]{0,63}", self.valves.DEFAULT_PROJECT_ID))
        )

    @staticmethod
    def _pseudonymous_requester(user: dict[str, Any] | None) -> dict[str, str] | None:
        user_id = user.get("id") if isinstance(user, dict) else None
        if not isinstance(user_id, str) or not user_id.strip():
            return None
        digest = hashlib.sha256(f"open-webui:{user_id}".encode("utf-8")).hexdigest()
        return {"source": "open-webui", "subject": f"owui-sha256:{digest}"}

    @staticmethod
    def _latest_user_question(body: dict[str, Any]) -> str:
        messages = body.get("messages")
        if not isinstance(messages, list):
            return ""

        for message in reversed(messages):
            if not isinstance(message, dict) or message.get("role") != "user":
                continue
            content = message.get("content")
            if isinstance(content, str):
                return content.strip()[:12_000]
        return ""

    @staticmethod
    def _answer(response: httpx.Response) -> str:
        try:
            data = response.json()
        except ValueError:
            return "PatchPony hat eine ungültige Workflow-Antwort erhalten."

        if not isinstance(data, dict):
            return "PatchPony hat eine ungültige Workflow-Antwort erhalten."

        answer = data.get("answer")
        if not isinstance(answer, str) or not answer.strip():
            return "PatchPony konnte keine Antwort erzeugen."

        return Pipe._render_transparency(answer.strip(), data.get("sources"), data.get("toolCalls"))

    @staticmethod
    def _render_transparency(answer: str, raw_sources: Any, raw_tools: Any) -> str:
        sources: list[tuple[str, str, int, int]] = []
        seen_sources: set[tuple[str, str, int, int]] = set()
        if isinstance(raw_sources, list):
            for item in raw_sources[:20]:
                if not isinstance(item, dict):
                    continue
                project_id = item.get("projectId")
                path = item.get("path")
                start_line = item.get("startLine")
                end_line = item.get("endLine")
                if not isinstance(project_id, str) or not re.fullmatch(r"[a-z0-9][a-z0-9-]{0,63}", project_id):
                    continue
                if not isinstance(path, str) or not re.fullmatch(r"[A-Za-z0-9._/@+:-]+(?:/[A-Za-z0-9._/@+:-]+)*", path):
                    continue
                if not isinstance(start_line, int) or not isinstance(end_line, int) or start_line < 1 or end_line < start_line:
                    continue
                source = (project_id, path, start_line, end_line)
                if source not in seen_sources:
                    seen_sources.add(source)
                    sources.append(source)

        tools: list[str] = []
        if isinstance(raw_tools, list):
            for tool in raw_tools[:10]:
                if tool in {"runtime.status", "runtime.validate_correlation", "source.search", "source.read"} and tool not in tools:
                    tools.append(tool)

        sections = [answer]
        if sources:
            rendered_sources = "\n".join(
                f"- `{project_id}:{path}:L{start_line}`" if start_line == end_line else f"- `{project_id}:{path}:L{start_line}-L{end_line}`"
                for project_id, path, start_line, end_line in sources
            )
            sections.append(f"Quellen (verifizierte Tool-Ergebnisse):\n{rendered_sources}")
        if tools:
            sections.append("Verwendete Read-only-Tools:\n" + "\n".join(f"- `{tool}`" for tool in tools))
        return "\n\n---\n\n".join(sections)