# SWIM bypass flow

SWIM can represent a simple preferential-flow shortcut from the surface to an
interior soil layer. This is an empirical option, not a simulation of crack
geometry, opening or closing. It requires calibration and validation against
observations for the intended soil and application.

Set `BypassFraction` between 0 and 1 and `BypassDepth` to a depth in mm. The
destination is the layer containing that depth; a depth exactly at a layer
boundary selects the layer below. The destination must be below the first
layer and above the bottom layer. A zero fraction (the default) disables the
pathway and does not require a depth.

For each SWIM timestep, the fraction is applied to incoming rainfall and
surface irrigation after any curve-number runoff has been removed. The amount
is capped by the destination layer's empty pore space at the beginning of the
timestep. The remainder goes to the ordinary surface pathway, where it may
infiltrate, pond or run off. As soil water redistributes, space can become
available for bypass in later timesteps. No separate crack storage is modelled.

Solute from the surface water pool is partitioned between bypass, matrix
infiltration, ponding and runoff. Bypass does not directly mobilise solute
already present in layers it passes through. The ordinary SWIM water and
solute equations continue to govern redistribution after entry into the
destination layer.

`BypassFlow` reports the daily water amount entering through this pathway, in
mm. Existing `Infiltration` and layer water/solute flow outputs remain matrix
flow outputs and do not include bypass flow. Add `BypassFlow` to `Infiltration`
when the combined net surface-to-soil flow is needed; the existing
`Infiltration` output is net of soil evaporation.

Bypass does not support a constant-potential surface boundary, simultaneous
upward surface flow, or subsurface irrigation. These combinations produce an
error rather than silently changing the water or solute balance.
