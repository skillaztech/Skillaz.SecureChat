Feature: Validation
  As application user
  I want to application validates my input
  So I won't save bad data into persistent storage

  Scenario: If user name 4 or less characters long then validation error should occur
    Given started instance
    When user set username between 0 and 4 chars length
    Then validation errors count should be 1

  Scenario: If user name 65 or more characters long then validation error should occur
    Given started instance
    When user set username between 65 and 1024 chars length
    Then validation errors count should be 1

  Scenario: If user name between 5 and 64 character length then no validation errors should occur
    Given started instance
    When user set username between 5 and 64 chars length
    Then validation errors count should be 0