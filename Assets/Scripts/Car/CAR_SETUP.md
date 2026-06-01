# Arcade Raycast Car Setup

The car uses one root `Rigidbody` and four raycast wheel mounts. Do not add
`WheelCollider` components or child rigidbodies.

## Prefab Hierarchy

```text
CarRoot
|- CarController
|- WheelConfig
|- PlayerDriver
|- CarVfxSfx (optional feedback)
|- Rigidbody
|- CarMesh
`- Wheels
   |- Wheel_FL
   |  `- WheelMesh
   |- Wheel_FR
   |  `- WheelMesh
   |- Wheel_RL
   |  `- WheelMesh
   `- Wheel_RR
      `- WheelMesh
```

Put `WheelPhysics` on each `Wheel_*` mount. Place every mount at its visible
tire center and assign its mesh child to `wheelMesh`.

Keep the physics mount fixed during play. `WheelPhysics` moves only the assigned
mesh child along the Rigidbody suspension axis, so imported wheel objects can
still animate correctly when their parent rotations or scales are unusual.

Wheel flags:

| Wheel | Front | Left |
| --- | --- | --- |
| FL | On | On |
| FR | On | Off |
| RL | Off | On |
| RR | Off | Off |

Assign the four mounts to `CarController.wheels`. `CarVfxSfx` auto-finds wheels
when its array is empty, but keeping the same wheel order as `CarController`
makes trail and particle assignment predictable.

Add a raised `BoxCollider` to `CarRoot` or a mesh child beneath it for obstacle
collisions and a realistic Rigidbody inertia tensor. Unity treats a child
collider as part of the root Rigidbody compound collider. Keep its lower edge
above the road so the suspension rays, rather than the body collider, support
the parked car. The `carTest` truck keeps this collider on its scaled `car mesh`
child so the collider follows that imported model cleanly.

## Ground

Every drivable road piece needs a non-trigger collider. A `BoxCollider` is fine
for a test floor. A `MeshCollider` or `TerrainCollider` is fine for roads and
terrain.

Create a `Ground`, `Drivable`, or `Floor` layer and select it in
`WheelConfig.groundMask`. The script can fall back to named layers and finally
default raycast layers for early setup, but explicitly assigning a mask avoids
self-collision surprises in production.

Keep the car hierarchy on `Default`. The wheel rays follow `Rigidbody.up`, not a
rotated wheel mesh axis.

## Rigidbody Starting Values

| Setting | Value |
| --- | --- |
| Mass | 1200 |
| Linear Damping | 0.05 |
| Angular Damping | 1.0 |
| Interpolate | Interpolate |
| Collision Detection | Continuous Dynamic |

The current truck model uses a `0.69` wheel radius. Measure and replace this
value when reusing the scripts on a different car mesh.

## Current Truck Tuning

These values are tuned for the imported truck model rather than a generic
one-meter placeholder:

| Setting | Value |
| --- | --- |
| Acceleration | 7500 |
| Throttle Response | 3.5 |
| Throttle Release Speed | 6 |
| Speed Limiter Response | 4 |
| Front Lift Drive Scale | 0.35 |
| Rear Lift Reverse Scale | 0.55 |
| Axle Support Reference | 0.45 |
| Max Steering Angle | 32 |
| Steering At Max Speed | 12 |
| Steering Response | 3.5 |
| Steering Return Speed | 6 |
| Turn Assist | 1500 |
| Turn Assist Response | 6 |
| Fallback Center Of Mass | `(0, -0.48, 0)` |
| Max Angular Speed | 2.5 |
| Downforce | 80 |
| Cornering Downforce | 85 |
| Roll Stabilize | 9500 |
| Roll Pitch Damping | 2200 |
| Yaw Damping | 1.25 |
| Front Anti-Roll | 9500 |
| Rear Anti-Roll | 8000 |
| Anti-Roll Response | 14 |
| Spring Strength | 34000 |
| Dampen Strength | 6800 |
| Max Suspension Force | 15000 |
| Rest Length | 0.14 |
| Spring Travel | 0.2 |
| Raycast Recovery Margin | 0.2 |
| Bump Start | 0.72 |
| Bump Strength | 12000 |
| Suspension Force Response | 28 |
| Corner Stiffness | 9000 |
| Max Lateral Mu | 1.3 |
| Front Grip Multiplier | 1.1 |
| Rear Grip Multiplier | 0.92 |
| Max Longitudinal Mu | 1.15 |
| Lateral Grip Response | 18 |
| Longitudinal Grip Response | 12 |
| Longitudinal Force Height Offset | 0.42 |
| Wheel Visual Follow Speed | 12 |
| Airborne Visual Droop | 0.18 |

`raycastRecoveryMargin` raises the ray origin slightly while preserving the
same suspension distance. This lets a wheel reacquire the floor after a hard
compression instead of continuing to miss once its mount has crossed the road
surface.

The steering input ramps toward its target instead of snapping instantly.
Front grip is slightly stronger than rear grip to reduce understeer, while
cornering downforce and roll damping keep the truck planted when the inside
wheels unload.

The response fields soften force transitions without replacing the raycast
physics. The project physics solver is set to `10` position iterations and `3`
velocity iterations so the chassis collider settles cleanly after impacts.

Longitudinal forces are applied slightly above the road contact point to avoid
levering the truck into wheelies. Drive power also fades as the leading axle
unloads. Grounded wheel meshes are positioned from `contact + normal * radius`,
which keeps the tire surface aligned with the road while reversing or pitching.

## Controls

`PlayerDriver` uses optional Input System action references. When they are
empty, it falls back to WASD or arrow keys, Space for braking, and a connected
gamepad left stick with the south button for braking.

## Optional Feedback

Assign looping engine and skid `AudioSource` components to `CarVfxSfx`.
Assign trails and particles in the same order as the wheels. Effects remain
disabled safely when these references are empty.

## Debug Rays

Enable Gizmos in the Scene view. `WheelConfig.drawDebugForces` draws:

- green or red: wheel ray hit or miss
- cyan: suspension force
- magenta: lateral grip
- blue: acceleration
- yellow: braking or coast drag
