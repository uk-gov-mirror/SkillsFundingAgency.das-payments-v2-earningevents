Feature: GSL Reprocess Pending Earnings

The Earnings Bridge should pick up GSL earnings that could not be published when they were first received,
and publish them once a relevant collection period opens, without republishing earnings already processed
for a collection period. See PV2-4229.

Scenario: Earnings received when no collection period open - Process pending earnings when a relevant collection period opens
Given no collection period is currently open
And a message is received for a Levy employer with a GSO learner
When the payments are generated
Then the earnings are stored in the Earnings Bridge cache table
When the collection period has opened recently
And the Earnings Bridge reprocesses pending earnings
Then the earnings are processed
And the outgoing GSL Earnings Event is published
And the earnings are marked as processed with a timestamp in the cache table and in the processing table for the current collection period


Scenario: Earnings received in a previous collection period - Re-process earnings in subsequent collection periods
Given the collection period has opened recently
And a message is received for a Levy employer with a GSO learner
When the payments are generated
When a new collection period opens
And the Earnings Bridge reprocesses pending earnings
Then the earnings are processed
And the outgoing GSL Earnings Event is published
And the earnings are marked as processed with a timestamp in the cache table and in the processing table for the current collection period


Scenario: Modify existing behaviour - Earnings received and processed in the current collection period
Given the collection period has opened recently
And a message is received for a Levy employer with a GSO learner
When the payments are generated
Then the earnings are stored in the Earnings Bridge cache table
And the earnings are processed
And the outgoing GSL Earnings Event is published
And the earnings are marked as processed with a timestamp in the cache table and in the processing table for the current collection period


Scenario: Do not reprocess earnings already processed for a collection period
Given the collection period has opened recently
And a message is received for a Levy employer with a GSO learner
When the payments are generated
Then the earnings are marked as processed with a timestamp in the cache table and in the processing table for the current collection period
When the Earnings Bridge reprocesses pending earnings
Then the earnings are not republished for that collection period
