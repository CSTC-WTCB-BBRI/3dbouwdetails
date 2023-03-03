<?php

$session = $_POST["session"];
$clientId = $_POST["clientId"];
$projectId = $_POST["projectId"];
$surfaces_ids = $_POST["ids"];
$tile_names = $_POST["tiles"];
$prices = $_POST["prices"];

$nb_choices = count($surfaces_ids);

try
{
	$bdd = new PDO('mysql:host=localhost;dbname=tpdemo;charset=utf8', 'root', '', array(PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION, PDO::MYSQL_ATTR_LOCAL_INFILE => true));
}
catch(Exception $e)
{
	die('Error : ' . $e->getMessage());
}

$choiceInsertion = "REPLACE INTO c" . $clientId . "_p" . $projectId . "_choices (id_surface, id_tile, session) ";
$choiceInsertion = $choiceInsertion . "SELECT c" . $clientId . "_p" . $projectId . "_surfaces.id_surface, tptiles.id, '" . $session . "' ";
$choiceInsertion = $choiceInsertion . "FROM c" . $clientId . "_p" . $projectId . "_surfaces, tptiles ";
$choiceInsertion = $choiceInsertion . "WHERE (libelle, c" . $clientId . "_p" . $projectId . "_surfaces.id_surface) IN (";

for($i = 0; $i < $nb_choices; $i++) {
	if ($i < $nb_choices - 1) {
		$choiceInsertion = $choiceInsertion . "('" . $tile_names[$i] . "', '" . $surfaces_ids[$i] . "'),";
	}
	else {
		$choiceInsertion = $choiceInsertion . "('" . $tile_names[$i] . "', '" . $surfaces_ids[$i] . "'));";
	}
}

$result = $bdd->query($choiceInsertion);

if ($result->errorCode() == 00000) 
{
  echo "User's choices table update: OK\r\n";
} 
else 
{
  echo "Error while updating the user's choices table!\r\n";
}

//Close the query access
$result->closeCursor();

// Now the price
$priceCmd = "UPDATE c" . $clientId . "_p" . $projectId . "_choices SET surface_price= case id_surface";

for($i = 0; $i < $nb_choices; $i++) {
	if ($i < $nb_choices - 1) {
		$priceCmd = $priceCmd . " WHEN '" . $surfaces_ids[$i] . "' THEN '" . $prices[$i] . "'";
	}
	else {
		$priceCmd = $priceCmd . " WHEN '" . $surfaces_ids[$i] . "' THEN '" . $prices[$i] . "' END WHERE id_surface IN ('";
		for($j = 0; $j < $nb_choices; $j++) {
			if ($j < $nb_choices - 1) {
				$priceCmd = $priceCmd . $surfaces_ids[$j] . "', '";
			}
			else
				$priceCmd = $priceCmd . $surfaces_ids[$j] . "');";
		}
	}
}

$result = $bdd->query($priceCmd);
if ($result->errorCode() == 00000) 
{
  echo "User's choices table update of price: OK\r\n";
} 
else 
{
  echo "Error while updating the price of the user's choices table!\r\n";
}
//Close the query access
$result->closeCursor();
?>