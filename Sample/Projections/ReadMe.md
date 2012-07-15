> Rinat Abdullin, 2012-07-15

This is a set of abstractions that demonstrate, how we can wire
view projections that denormalize events into persistent read models (views).

For deeper detail in this area, visit Lokad.CQRS project, which features
multuple document store implementations (places, where views are persisted) and
also automatic projection management code (which automatically rebuilds
views, if projection code has changed).