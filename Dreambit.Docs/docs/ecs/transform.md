# Transform

Every entity has a `Transform`. Local values are relative to the parent;
world values include the complete parent chain.

```csharp
Transform.Position2D = new Vector2(20, 10);       // parent space
Transform.WorldPosition2D = new Vector2(200, 80); // world space
Transform.Scale2D = new Vector2(2, 2);
Transform.Rotation2D = MathHelper.ToRadians(45);
```

Use radians for all rotations. In Dreambit's axis convention local `+X` is
forward, `+Y` is right, and `+Z` is up. The 2D helpers rotate around Z.

## Moving and aiming

```csharp
Transform.TranslateWorld2D(velocity * Time.DeltaTime);
Transform.MoveForward2D(speed * Time.DeltaTime);
Transform.RotateTowardsPoint2D(target, turnSpeed * Time.DeltaTime);
Transform.LookAt2D(mouseWorldPosition);
```

`Translate2D` changes local/parent-space position. `TranslateWorld2D` changes
world position and is normally the clearer choice for gameplay movement.

## Coordinate conversion

```csharp
Vector2 muzzleWorld = Transform.TransformPoint2D(localMuzzle);
Vector2 hitLocal = Transform.InverseTransformPoint2D(hitWorld);
Vector2 facingWorld = Transform.TransformDirection2D(Vector2.UnitX);
```

Point conversion includes translation, rotation, and scale. Direction conversion
uses rotation only.

## Parenting cautions

World setters compute the correct local value under a parent. A zero component
in a parent scale makes the inverse calculation impossible and throws. Set the
parent before setting final world position if you are assembling a hierarchy.

`LastWorldPosition2D` is maintained for physics snapshots. Components normally
should not manage it unless implementing custom swept physics.

