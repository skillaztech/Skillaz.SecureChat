Feature: Initialize
  As application user
  I want to application starts without errors
  
  Scenario: All properties in user settings initialized correctly
    When application starts
    Then user id should be not empty
    Then user name should be not empty
    Then secret code should be not empty

  Scenario Outline: Unix sockets folder should set properly for different OS
    Given <OS> operation system
    When application starts
    Then unix sockets folder should be <Unix socket folder>
    
    Examples:
    | OS      | Unix socket folder |
    | Windows | ProgramData        |
    | Linux   | tmp                |
    | MacOS   | tmp                |