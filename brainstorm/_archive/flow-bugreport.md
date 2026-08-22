Bugreports und Feature Requests



Hier könnte CodeCell sogar einen sehr schönen Workflow haben:



issue.analyze(...)



intern:



1\. Projekt-Metadaten lesen

2\. passende Skills suchen

3\. relevanten Source finden

4\. relevante Config finden

5\. Tests finden

6\. Abhängigkeiten bestimmen



Output:



summary: ...

likely\_files:

&#x20; - src/foo/service.py

&#x20; - src/foo/model.py





related\_config:

&#x20; - config/foo.yaml





tests:

&#x20; - tests/foo/test\_service.py





implementation\_plan:

&#x20; - ...

&#x20; - ...

&#x20; - ...





risks:

&#x20; - ...



Danach optional:



issue.implement(...)



Das Projekt wäre dann weniger



„LLM darf auf meinen Rechner“



und mehr



„Ein standardisiertes, isoliertes Projekt-Environment, das sich einem Agent über MCP präsentiert.“



Das ist meiner Meinung nach die bessere Produktidee.

