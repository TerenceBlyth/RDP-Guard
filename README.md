# RDP-Guard
Windows service to block failed logon attempts
The service is notified on a logon failure and then creates a rule in Windows Firewall to block the attacker.
You can add ipaddress to a whitelist file and it wont block them.
You can read any blocks it has made in the Windows application log, Eventviewer.
Any comments of changes you can email me terence@ctec.co.nz
