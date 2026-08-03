# AI

Dreambit includes two independent AI building blocks:

- An eight-neighbor A* grid/pathfinder/follower built around an LDtk IntGrid.
- A component-based finite state machine with states, guarded transitions,
  events, history, and a blackboard.

They can be combined: an FSM state can ask `AStarPathFollower` to seek the
player, pause it on exit, and use blackboard values in transition guards.

The A* components have a current convention: the initialized `AStarGrid` must be
attached to a scene entity named `managers`. Treat this as part of their setup
contract.

