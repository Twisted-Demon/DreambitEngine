# ECS folder guide

The ECS code is organized by responsibility. File location does not determine
the C# namespace; ECS types continue to use the existing `Dreambit.ECS`
namespace.

## Core

`Core` contains the ECS foundation rather than game-facing component types:

- `Attributes` contains attributes used to declare component relationships.
- `Repositories` contains entity and component storage/indexing.
- `Requirements` contains required-component resolution.
- The root of `Core` contains the base component, drawable component, entity,
  and transform types.

Add a file here only when it implements ECS mechanics used across component
domains.

## Components

`Components` contains reusable engine components grouped by their purpose:

- `AI` for navigation and behavior components.
- `Audio` for emitters and listeners.
- `Camera` for camera components.
- `Physics` for collision, movement, and rigid-body components.
- `Rendering` for visible components and closely related support types, split
  into lighting, particles, shapes, and sprites.
- `UI` for user-interface components.

Put a new component in the closest existing domain. Create a new domain folder
when the component represents a distinct engine feature rather than forcing it
into an unrelated category.

## Legacy

`Legacy` contains retained historical implementations that are not part of the
current ECS structure. New components and ECS infrastructure should not be
added here.
