<?php

$clientId = $_POST["clientId"];
$projectId = $_POST["projectId"];
$name = $_POST["name"];
$tilesTable = "tptiles";

try
{
	$bdd = new PDO('mysql:host=localhost;dbname=tpdemo;charset=utf8', 'root', '', array(PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION, PDO::MYSQL_ATTR_LOCAL_INFILE => true));
}
catch(Exception $e)
{
	die('Error : ' . $e->getMessage());
}

$getName = "SELECT * FROM `" . $tilesTable . "` WHERE `libelle`='" . $name . "'";
$result = $bdd->query($getName);

if ($result->errorCode() == 00000) 
{
  echo "User's choices table creation: OK\r\n";
} 
else 
{
  echo "Error while creating the user's choices table!\r\n";
}

if ($result->rowCount() > 0) {
    echo "RETURNS\r\n";
}

// output data of each row
while($row = $result->fetch(PDO::FETCH_ASSOC)) {
    echo ";" . $row['chemin_texture'];
} 

//Close the query access
$result->closeCursor();
?>