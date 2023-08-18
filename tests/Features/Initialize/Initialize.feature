Feature: Initialize
  As application user
  I want to application starts properly and without errors
  
  Scenario: Application starts without errors
    Given default application configuration
    When application starts
    Then no errors occurs
    
  Scenario: If application port is not busy then remote listener and local listener both should bound on sockets
    Given default application configuration
    When application starts
    And initial commands executes
    Then remote listener is bounded
    And local listener is bounded