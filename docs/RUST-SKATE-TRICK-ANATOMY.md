# Trick anatomy: what the rider's feet and body do

This is the reference for how each trick has to look. Read the section for a
trick before you author, repair or review its animation. A capture source shows
one attempt from one camera; this page says what every attempt of that trick has
in common, so a source gap is filled with the right motion and not with a
generic one. The owner reviews against this page. Points marked *(owner to
confirm)* are best knowledge that the owner has not checked yet.

It complements [the visible-result rule](AUTONOMY.md#visible-result-over-source-fidelity-owner-28-october-2026):
a believable pose beats fidelity to an unreliable source, and this page is what
"believable" means for each trick.

## Phases

Every flatground trick runs through the same phases:

1. **Setup.** Feet move from the riding stance to the trick's own start
   position. In the game the trick is only known when the flick input arrives,
   so the rider charges in a neutral crouch and the feet shift to the trick's
   start position in the first frames of the pop (owner, 29 October 2026). The
   shift is short and smooth, and the feet stay on the deck. It adds no delay
   before the pop.
2. **Load.** Knees and hips bend; the weight sits over the board.
3. **Pop.** The popping foot snaps the tail (or the nose for nollie) to the
   ground. The body rises from the legs, not from a jump with straight legs.
4. **Flick or scoop.** The other foot (or the popping foot, for shove-its)
   gives the board its rotation. The board starts rotating when the foot
   leaves the board, not before.
5. **Air.** Both knees tuck up together and the feet stay above the board,
   close together and roughly over the bolts, with just enough room (5 to 10 cm)
   for the turning deck and trucks. One leg must never shoot forward while the
   other stays straight.
6. **Catch.** Both feet come back onto the grip over the bolts, at about the
   same time.
7. **Land.** Knees absorb the impact over about 0.15 s. No snap in one frame.
8. **Roll-away.** Knees stay soft; the rider is at least as low as in Load,
   both soles on the deck, both feet at least 80 % over the deck.

## Measurable checks

The inspector at `http://127.0.0.1:8800/inspect.html` (owner's machine) measures
these for every exported clip. A candidate is not shown to the owner until they
pass:

- No leg or foot vertex inside the deck between Pop and Catch.
- In Air, the two feet differ less than 10 cm in height and both stay within
  the length of the deck.
- After landing, both soles 0 to 2 cm above the deck and at least 80 % over it.
- No hip acceleration spike above 120 m/s² (pop and land are spread over a few
  frames).
- Start positions differ per family (two families with identical start
  numbers is a failure).
- Flatground wheel height at the top of the jump 40 to 60 cm (owner, 29 October
  2026).

The flick itself cannot be measured this way. Compare it frame by frame with
the source at 0.25× speed, zoomed in on the flicking foot.

## Pop and jump

- **Flatground height:** wheels 40 to 60 cm above the ground at the top. Normal
  gravity (about 8 to 9.8 m/s²) and about 0.6 s of air. A lower pop with very low
  gravity looks like the moon and is wrong (owner, 29 October 2026).
- **Ollie:** back foot with the ball of the foot on the tail. Front foot just
  behind the front bolts, across the deck. On the pop the front foot drags up
  the grip toward the nose with the outer side of the shoe, which levels the
  board. Both knees tuck.

## Flip family

### Kickflip

- **Start:** front foot angled about 45° just behind the front bolts. Back
  foot with the ball of the foot on the tail.
- **Flick:** the front foot drags up the grip toward the nose. Near the nose
  the ankle rolls, and the **instep, the top and outer side of the shoe, not the
  sole**, catches the heel-side edge of the nose and sweeps diagonally off the
  board (owner: "flicks are with the instep, not with the sole"). The board
  starts rolling as the foot leaves the edge.
- **Air:** front foot comes straight back over the board, knees tucked
  together.
- **Fakie kickflip:** same feet, rolling backward; the tail (now leading)
  pops.

### Heelflip

- **Start:** front foot straighter across the deck than for a kickflip, with
  the toes over the toe-side edge so the heel can kick. Looks clearly different
  from the kickflip setup.
- **Flick:** the front foot drags up, then the **heel** kicks forward and
  diagonally off the toe-side corner of the nose. The board rolls the other way
  from a kickflip.
- **Nollie, switch and nollie backside 180 heelflip:** the same heel kick,
  from the matching stance (see [stances](#stances)).

### Hardflip

- Frontside pop shove-it scoop with the back foot plus a kickflip flick with
  the front foot. The board passes vertically between the legs, so the legs open
  enough to let it through; the front knee tucks high. *(owner to confirm the
  exact back-foot corner)*

### 360 flip

- **Start:** back foot on the toe-side corner of the tail (the scoop pocket),
  front foot in a kickflip position, a little further back.
- **Scoop and flick:** the back foot scoops the tail backward (backside 360
  shove) while the front foot gives a small kickflip flick. Most of the
  rotation comes from the back foot.

### Impossible

- **Start:** back foot near the middle of the tail; front foot further back
  toward the middle of the board than for an ollie.
- **Wrap:** the front foot leaves the board entirely; the board stays against
  the back foot and wraps one full vertical turn around it (nose up, over the
  front of the foot, under and back). The back foot is the pivot and never
  leaves the board.

## Shove-it family

- **Backside pop shove-it:** back foot on the toe-side corner of the tail;
  it pops and scoops the tail backward (behind the rider). The front foot lifts
  straight up out of the way and does not flick.
- **Frontside pop shove-it:** back foot on the heel-side corner of the tail;
  it pops and pushes the tail forward (in front of the rider). Front foot as
  above.

## Stances

- **Regular / goofy:** mirror images; a fault in one is checked in the other.
- **Nollie:** the nose pops. The front foot sits with the ball of the foot on
  the nose; the back foot sits around the back bolts and does the flick (a
  nollie kickflip is flicked by the back foot).
- **Fakie:** riding backward; the tail is leading and still pops.
- **Switch:** the opposite stance from the rider's own, so the whole setup is
  mirrored, not the normal setup reused.

## Arms

Arms move for balance: out to the side and slightly forward during Pop and Air,
following the shoulders. Never both straight above the head, never snapping in
one frame, never through the body.
