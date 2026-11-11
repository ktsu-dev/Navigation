## v1.5.2 (patch)

Changes since v1.5.1:

- Refuse to save a subclass item that JSON would save as its base type [patch] (#113) (matt-edmondson)
- Record nothing when LoadStateAsync loads the current stack [patch] (#110) (matt-edmondson)
- Roll back an undo or redo whose NavigationChanged handler throws [patch] (#112) (matt-edmondson)
- Keep a state file's permissions and symbolic link when saving over it [patch] (#114) (matt-edmondson)
- Add a SessionStart hook for Claude Code cloud sessions (#91) (matt-edmondson)
- Save navigation state through a temporary file [patch] (#105) (matt-edmondson)
- Report each navigation once when a provider executes actions as it registers them [patch] (#103) (matt-edmondson)
- Merge remote-tracking branch 'origin/main' into fix/95-redirect-during-undo (matt-edmondson)
- Track restoring threads on the stack instead of in a static field (matt-edmondson)
- Explain the empty redo loop in the redirect test (matt-edmondson)
- Use the library's real API names throughout docs/ [patch] (matt-edmondson)
- Don't record a redirect made by a NavigationChanged handler during undo or redo [patch] (matt-edmondson)
- Read the history and current index in one step in FromNavigationStack [patch] (matt-edmondson)
- Return null from LoadStateAsync for an item type JSON can't construct [patch] (Claude)
- Reject a loaded state with no current item but items, or any invalid state from a custom provider [patch] (Claude)

