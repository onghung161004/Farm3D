# Bridge railing collision

Requested behavior: unrepaired bridge has no side safety barrier and player can step off into water; repaired bridge's visible rails block lateral movement. Both entrances remain open. Upgraded widths2x/3x move the collision rails with the actual walkable mesh, no invisible wall left at old width. Reuse repair flag and save8, no new charge or state.

Implement generated BoxCollider segments for left/right chains of paired walkway rows; non-trigger, above deck and sloped along each segment, no rendered replacements. Disabled until repair, enabled on repair and saved-state reload. Guard updates only on mesh/state change. Physics/navigation see the same side obstacles; water/rescue mechanics are not redesigned. No jump-proof roof or end barrier.
