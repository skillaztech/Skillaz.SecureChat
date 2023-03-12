Feature: Initialize
  As application user
  I want to application starts without errors
  
  Scenario: All properties in user settings initialized correctly
    When application starts
    Then user id should be not empty
    Then user name should be not empty
    Then secret code should be not empty