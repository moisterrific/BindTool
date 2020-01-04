# BindTool

An update on InanZen's AdminTools BindTool command.

Enables the binding of commands to items in hotbar or cursor.

/bindtool (/bt) command:
/bindtool [-flags] commands; separated; by semicolon.
Available flags:
-l will loop trough commands in order
-s will bind item only at certain slot
-p will bind item only with certain prefix
-d will add bind to database, so it will be saved and can be used after rejoin
-w instead of execution will add command to queue, so you could add parameters later
-c will clear all commands from the item at certain slot with certain prefix
-csp = clear any bind on item
-cs = clear binds on item with certain prefix, but any slot
-cp = clear binds on item with certain slot, but any prefix
You can combine flags:
-spd = slot + prefix + database
-wd = awaiting + database
/bindtool list - shows all active binds with full information
/bindtool help - this information
/bindtool help wait - information about queued commands

/bindwait (/bw) command:
Syntax:
Text format uses {Num} syntax. Parameters start from 0.
For example /bt /region allow "{0}" "Region Name" will allow you to use /bw Player1, /bw "Player 2", etc.
/bindwait - shows current awaiting command
/bindwait list - shows all queued commands
/bindwait skip <count / all> - removes commands from queue
/bindglobal (/bgl) command:
/bindglobal add [Name] [ItemID] [Permission] [SlotID] [PrefixID] [Looping] [Awaiting] commands; separated; by semicolon - adds global bind
SlotID: -1 for any; 1-10 - hotbar; 100 for cursor
PrefixID: -1 for any
Looping and Awaiting: true/false
Requires bindtools.admin permission
/bindglobal del [Name] - removes global bind
Requires bindtools.admin permission
/bindglobal list - shows all global binds you allowed to use
/bprefix (/bpr) command:
/bindprefix [PrefixID] - changes item's prefix you hold
/bindprefix add group [Name] [Permission] [AllowedPrefixes (1 3 10...)] - adds prefix group
Requires bindtools.admin permission
/bindprefix del group [Name] - removes prefix group
Requires bindtools.admin permission
/bindprefix <add/del> prefix [Name] [PrefixID] - adds/removes prefix in prefix group
Requires bindtools.admin permission
/bindprefix list - shows all prefixes you allowed to use
/bindprefix listgr - shows all prefix groups
Requires bindtools.admin permission

Binds info:
You can create few binds on same item, if using different slots or prefixes
Binds can be overwritten:
If you create general bind, all specific binds will be removed
If you create specific bind, general bind will be removed
You can't overwrite global bind if you don't have bindtools.overwrite permission
You can't use all prefixes in /bprefix command if you don't have bindtools.allowprefix permission or permission of certain prefix group.

bindtools.command.bind - /bindtool
bindtools.command.wait - /bindwait
bindtools.command.global - /bindglobal
bindtools.command.prefix - /bprefix
bindtools.admin - allows you to manage global binds and prefix groups
bindtools.overwrite - allows you overwrite global binds with /bindtool
bindtools.allowprefix - allows you to use all prefixes in /bprefix command 

https://tshock.co/xf/index.php?resources/bindtools.194/
