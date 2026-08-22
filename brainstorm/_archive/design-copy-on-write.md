Ein Design würde ich unbedingt einbauen: Copy-on-Write



Anstatt den echten Workspace direkt zu verändern:



project/

&#x20;   original



bekommt jeder Agent:



base project      READ ONLY

&#x20;      │

&#x20;      ▼

copy-on-write layer

&#x20;      │

&#x20;      ▼

agent workspace



Der Agent kann darin beliebig:



editieren

löschen

formatieren

tests laufen lassen



und am Ende bekommst du nur:



git diff



bzw.



patch



zurück.



boxsh macht genau diesen Copy-on-Write-Ansatz bereits, was ein gutes Indiz dafür ist, dass das Security-/UX-Muster sinnvoll ist.



Ich würde das bei deinem Projekt fast zu einem Kernprinzip machen.

