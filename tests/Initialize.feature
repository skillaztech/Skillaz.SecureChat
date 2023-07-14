Feature: Initialize
  As application user
  I want to application starts without errors
  
  Scenario: Application starts without errors
    Given default application configuration
    When application starts
    Then no errors occurs