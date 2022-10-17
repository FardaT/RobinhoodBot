# RobinhoodBot

Sample project created for learning purposes using Bot Framework v4 and and an empty bot template.
The application intends to mimik a type of conversation that could take place on a trading platform.

Further information on [Bot Framework](https://dev.botframework.com).

## Features

The sample features 2 conversation journeys:

#### Question Answering
In case chat input is recognized as a questions towards trading, then the the corresponding answer from the question answering databse will be returned.
When answered the initial dialogue is prompted where users may ask other question or terminate the conversation.

#### Trading dialogue
If intent can be derived with sufficient confidence score, then sale/purchase dialogue will be initiated. In this dialogue, the asset, the name of the asset as well as the price and quantity at which they should be bought are being requested.
If all the steps of the dialogue are executed then users may ask other question or terminate the conversation.

#### Stock data
Asset name / ticker symbol are being recognized manually, API connection to fetch current market data is not yet implemented.

#### Test
Tested with Bot Framework Emulator version 4.3.0