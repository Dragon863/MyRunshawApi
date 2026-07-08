#!/bin/bash

# Database settings; adjust accordingly
DB_HOST="localhost"
DB_PORT="5432"
DB_NAME="myrunshaw_db"
DB_USER="postgres"

TABLE_NAME="\"InAppNotices\""
COL_ID="\"NoticeId\""
COL_ANDROID="\"Android\""
COL_IOS="\"Ios\""
COL_TITLE="\"Title\""
COL_DESC="\"Description\""
COL_EXPIRES="\"Expires\""
COL_IMAGE="\"ImageUrl\""
COL_LINK="\"Link\""
COL_LINK_TEXT="\"LinkText\""
COL_MIN="\"MinVersion\""
COL_MAX="\"MaxVersion\""

echo "In-App Notice Creation Script"

read -p "Notice ID (e.g., maintenance-01): " notice_id
if [ -z "$notice_id" ]; then
    echo "Error: Notice ID is required."
    exit 1
fi

read -p "Title: " title
if [ -z "$title" ]; then
    echo "Error: Title is required."
    exit 1
fi

read -p "Description: " description
if [ -z "$description" ]; then
    echo "Error: Description is required."
    exit 1
fi

read -p "Show on Android? (Y/n): " show_android
show_android=${show_android:-Y}

read -p "Show on iOS? (Y/n): " show_ios
show_ios=${show_ios:-Y}

read -p "Expires in how many days? (default 7): " expires_days
expires_days=${expires_days:-7}

read -p "Optional Image URL (Enter to skip): " image_url
read -p "Optional External Link (Enter to skip): " link
read -p "Optional Link Text (Enter to skip): " link_text
read -p "Optional Minimum App Version (Enter to skip): " min_version
read -p "Optional Maximum App Version (Enter to skip): " max_version

# map Y/n to boolean
android_val="true"
if [[ "$show_android" =~ ^[nN]$ ]]; then android_val="false"; fi

ios_val="true"
if [[ "$show_ios" =~ ^[nN]$ ]]; then ios_val="false"; fi

format_nullable() {
    if [ -z "$1" ]; then
        echo "NULL"
    else
        echo "\$\$$1\$\$"
    fi
}

img_sql=$(format_nullable "$image_url")
link_sql=$(format_nullable "$link")
link_text_sql=$(format_nullable "$link_text")
min_sql=$(format_nullable "$min_version")
max_sql=$(format_nullable "$max_version")

SQL_QUERY="
INSERT INTO ${TABLE_NAME} (
    ${COL_ID}, ${COL_ANDROID}, ${COL_IOS}, ${COL_TITLE}, ${COL_DESC}, ${COL_EXPIRES}, ${COL_IMAGE}, ${COL_LINK}, ${COL_LINK_TEXT}, ${COL_MIN}, ${COL_MAX}
) VALUES (
    \$\$$notice_id\$\$, 
    ${android_val}, 
    ${ios_val}, 
    \$\$$title\$\$, 
    \$\$$description\$\$, 
    NOW() + INTERVAL '${expires_days} days', 
    ${img_sql}, 
    ${link_sql}, 
    ${link_text_sql}, 
    ${min_sql}, 
    ${max_sql}
)
ON CONFLICT (${COL_ID}) DO UPDATE SET
    ${COL_ANDROID} = EXCLUDED.${COL_ANDROID},
    ${COL_IOS} = EXCLUDED.${COL_IOS},
    ${COL_TITLE} = EXCLUDED.${COL_TITLE},
    ${COL_DESC} = EXCLUDED.${COL_DESC},
    ${COL_EXPIRES} = EXCLUDED.${COL_EXPIRES},
    ${COL_IMAGE} = EXCLUDED.${COL_IMAGE},
    ${COL_LINK} = EXCLUDED.${COL_LINK},
    ${COL_LINK_TEXT} = EXCLUDED.${COL_LINK_TEXT},
    ${COL_MIN} = EXCLUDED.${COL_MIN},
    ${COL_MAX} = EXCLUDED.${COL_MAX};
"

echo "Inserting notice..."

psql -h "$DB_HOST" -p "$DB_PORT" -d "$DB_NAME" -U "$DB_USER" -c "$SQL_QUERY"

if [ $? -eq 0 ]; then
    echo "Notice successfully created/updated in the database!"
else
    echo "Failed to create notice. Check database connection and casing settings."
fi