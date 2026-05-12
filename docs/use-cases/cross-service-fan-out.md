# Use Case: Cross-Service Event Fan-Out

## Goal
When an expense is added to a household, every active member of that household (except the creator) receives a notification — even though the notifications service has no direct knowledge of household membership.

## How projections solve this

The notifications service maintains a local `household_member_projections` read-model, populated by consuming finance events:

| Event | Projection change |
|---|---|
| `FinanceHouseholdCreatedEvent` | Insert owner as active member |
| `FinanceHouseholdMemberJoinedEvent` | Upsert member as active |
| `FinanceHouseholdMemberLeftEvent` | Mark member inactive |
| `FinanceHouseholdMemberRemovedEvent` | Mark member inactive |

When `FinanceExpenseCreatedEvent` arrives, the consumer queries this projection to get the current active member list and publishes one notification per member (excluding the creator).

## Forum thread/comment fan-out

The same pattern applies for forum comment notifications. `thread_author_projections` and `comment_author_projections` are built incrementally from `ForumThreadCreatedEvent` and `ForumCommentCreatedEvent` respectively. If a `ForumCommentCreatedEvent` arrives before the corresponding `ForumThreadCreatedEvent` has been processed, the consumer throws so MassTransit retries and resolves the ordering race.
