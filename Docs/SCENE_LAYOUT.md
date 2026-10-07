# Provisional scene layout

Dimensions and placements are design suggestions, to validate in the headset.

## Geometry

Showroom approximately 10 m wide by 8 m deep, ceiling approximately 3 m. Rear safe room approximately 3 m by 3 m, attached to the showroom. Add a short pavement along the front facade.

The front faces the street. A large glazed window occupies much of that facade, with a separate entrance. Keep a broad navigable route through the room and avoid placing evidence where it cannot be reached comfortably.

## Contents

- Two central display islands.
- Wall display cabinets.
- Service counter near the rear.
- Doorway to a small safe room.
- Large front window with static broken-glass pieces.
- Fixed evidence props and a separate group for user-deployed tools.

## Crime state

- Broken street-facing window with shards near the opening.
- Damaged glass on selected display cases.
- Empty jewelry stands mixed with remaining stock.
- Shoe impression on a readable floor surface.
- Open safe in the rear room.
- Discarded torch equipment near the safe; surrounding damage should match the final fictional scenario.

No exact forensic interpretation is built into the software.

## Suggested Unity hierarchy

```
CrimeScene
  Environment
  FixedEvidence
  Lighting
  XR Origin
  ToolStation
  DeployedTools
  DemoUI
  SessionManager
```

Keep intact/damaged variants reusable for a later comparison. Static glass pieces are sufficient for the first demo.
