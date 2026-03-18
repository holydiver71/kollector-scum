Proposed Add Release Step 5 Image search flow.

1. User navigates to step 5 in the wizard through normal manual add release or edit release steps.
2. User presented with 2 options 
    a. Search the web
    b. Upload

Search the web:
User is presented with up to 4 best match search results for the album being added. Use appropriate meta data entered in the wizard to refine the search (artist, title, cat number, format, countrym etc). 4 search results are shown in a grid Image and key also key meta data to differentiate them from each other (the images may look very similar)
User can then select the image to download or choose to do a manual upload if they are not happy with the results presented to them.

Upload:
User is presented with a themed file picker upload pop-up. Only image files can be uploaded.

Any images added to a relase either through upload or download must adhere to the current image size restrictions imposed already.
In the event google search API is used as the fall back we need to ensure the free tier limit of 100 requests is strictly adhered to. In the event that the daily limit has already been reached then the user is shown a popup explaining that there is a 100 image search limit imposed on the app daily that has been reached. Apologise to the user and ask them to try again tomorrow.
