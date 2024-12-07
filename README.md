# rest-api-fs
for learning purposes

## Create User
```text
curl --location --request POST 'http://localhost:6012/users' \
--header 'Content-Type: application/x-www-form-urlencoded' \
--data-urlencode 'name=Yuda'
```

## Read Users
```text
curl --location --request GET 'http://localhost:6012/users'
```

## Update User
```text
curl --location --request PUT 'http://localhost:6012/users' \
--header 'Content-Type: application/x-www-form-urlencoded' \
--data-urlencode 'id=1' \
--data-urlencode 'name=Fajar'
```

## Delete User
```text
curl --location --request DELETE 'http://localhost:6012/users' \
--header 'Content-Type: application/x-www-form-urlencoded' \
--data-urlencode 'id=1'
```