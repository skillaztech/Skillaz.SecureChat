Feature: Settings
  As application user
  I want to be able to setup application settings

  Scenario: When user changes secret code it should be saved
    Given started application instance with secret code 100000
    And secret code set by user is 120351
    When user saves user settings
    Then saved secret code should be equal to 120351

  Scenario: When user changes username it should be saved
    Given started application instance with username 'Johnny'
    And username set by user is 'Bobby'
    When user saves user settings
    Then saved username should be equal to 'Bobby'