# Use Case: Cross-Service Event Fan-Out

## Goal
When a bill is added to a household, every active member of that household (except the creator) receives a notification — even though the notifications service has no direct knowledge of household membership.

## How projections solve this

The notifications service maintains a local `household_member_projections` read-model, populated by consuming bills events:

| Event | Projection change |
|---|---|
| `BillsHouseholdCreatedEvent` | Insert owner as active member |
| `BillsHouseholdMemberJoinedEvent` | Upsert member as active |
| `BillsHouseholdMemberLeftEvent` | Mark member inactive |
| `BillsHouseholdMemberRemovedEvent` | Mark member inactive |

When `BillsBillCreatedEvent` arrives, the consumer queries this projection to get the current active member list and publishes one notification per member (excluding the creator).

## Forum thread/comment fan-out

The same pattern applies for forum comment notifications. `thread_author_projections` and `comment_author_projections` are built incrementally from `ForumThreadCreatedEvent` and `ForumCommentCreatedEvent` respectively. If a `ForumCommentCreatedEvent` arrives before the corresponding `ForumThreadCreatedEvent` has been processed, the consumer throws so MassTransit retries and resolves the ordering race.
