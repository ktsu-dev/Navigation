## v1.5.2 (patch)

Changes since v1.5.1:

- Merge remote-tracking branch 'origin/main' into fix/95-redirect-during-undo (matt-edmondson)
- Track restoring threads on the stack instead of in a static field (matt-edmondson)
- Explain the empty redo loop in the redirect test (matt-edmondson)
- Use the library's real API names throughout docs/ [patch] (matt-edmondson)
- Don't record a redirect made by a NavigationChanged handler during undo or redo [patch] (matt-edmondson)
- Read the history and current index in one step in FromNavigationStack [patch] (matt-edmondson)
- Return null from LoadStateAsync for an item type JSON can't construct [patch] (Claude)
- Reject a loaded state with no current item but items, or any invalid state from a custom provider [patch] (Claude)

